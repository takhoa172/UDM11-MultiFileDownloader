using System.Net.Sockets;
using Shared;

namespace Server;

public sealed class UploadHandler : IDisposable
{
    private const long MaxUploadSize = 3L * 1024 * 1024 * 1024;

    private readonly NetworkStream _stream;
    private readonly string _storagePath;

    private FileStream? _fileStream;
    private string? _tempFilePath;
    private string? _fileName;
    private string? _expectedHash;

    private long _totalSize;
    private long _receivedBytes;
    private int _expectedChunkIndex;
    private int _totalChunks;

    private bool _uploadStarted;
    private bool _uploadCompleted;

    public UploadHandler(
        NetworkStream stream,
        string storagePath)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _storagePath = storagePath ?? throw new ArgumentNullException(nameof(storagePath));

        Directory.CreateDirectory(_storagePath);
    }

    // ============================================================
    // UPLOAD_REQ
    // ============================================================

    public async Task HandleUploadRequestAsync(
        ProtocolPacket request,
        CancellationToken cancellationToken = default)
    {
        if (_uploadStarted && !_uploadCompleted)
        {
            await SendErrorAsync(
                "409_UPLOAD_IN_PROGRESS",
                "Máy chủ đang xử lý một tệp tải lên khác.",
                cancellationToken);

            return;
        }

        string? validationError =
            PacketValidator.ValidateUpload(
                request.FileName,
                request.TotalSize);

        if (validationError != null)
        {
            await SendErrorAsync(
                validationError,
                GetUploadValidationMessage(validationError),
                cancellationToken);

            return;
        }

        if (request.TotalSize < 0)
        {
            await SendErrorAsync(
                "400_INVALID_SIZE",
                "Kích thước tệp không hợp lệ.",
                cancellationToken);

            return;
        }

        if (request.TotalSize > MaxUploadSize)
        {
            await SendErrorAsync(
                "413_FILE_TOO_LARGE",
                "Tệp vượt quá giới hạn 3 GB.",
                cancellationToken);

            return;
        }

        string fileName = Path.GetFileName(request.FileName!);

        string finalPath =
            Path.Combine(_storagePath, fileName);

        // Không ghi đè file cũ.
        if (File.Exists(finalPath))
        {
            await SendErrorAsync(
                "409_FILE_EXISTS",
                $"Tệp '{fileName}' đã tồn tại trên máy chủ.",
                cancellationToken);

            return;
        }

        try
        {
            _fileName = fileName;
            _expectedHash = request.FileHash;
            _totalSize = request.TotalSize;
            _receivedBytes = 0;
            _expectedChunkIndex = 0;
            _totalChunks = request.TotalChunks;

            _tempFilePath = CreateTempFilePath(fileName);

            _fileStream = new FileStream(
                _tempFilePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                ServerConfig.BufferSize,
                useAsync: true);

            _uploadStarted = true;
            _uploadCompleted = false;

            ServerLogger.LogInfo(
                $"Bat dau upload file '{fileName}', size={_totalSize} bytes.");

            await SendPacketAsync(
                new ProtocolPacket
                {
                    Command = PacketCommand.UPLOAD_REQ,
                    Success = true,
                    FileName = fileName,
                    Message = "Máy chủ đã sẵn sàng nhận tệp."
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            await CleanupUploadAsync();

            FileErrorInfo error =
                FileErrorHandler.FromException(
                    ex,
                    fileName,
                    _tempFilePath ?? finalPath);

            await SendErrorAsync(
                error.ErrorCode,
                error.Message,
                cancellationToken);
        }
    }

    // ============================================================
    // UPLOAD_CHUNK
    // ============================================================

    public async Task HandleUploadChunkAsync(
        ProtocolPacket request,
        CancellationToken cancellationToken = default)
    {
        if (!_uploadStarted || _fileStream is null)
        {
            await SendErrorAsync(
                "400_UPLOAD_NOT_STARTED",
                "Chưa có phiên tải lên.",
                cancellationToken);

            return;
        }

        if (_uploadCompleted)
        {
            await SendErrorAsync(
                "409_UPLOAD_COMPLETED",
                "Phiên tải lên đã kết thúc.",
                cancellationToken);

            return;
        }

        if (request.ChunkIndex != _expectedChunkIndex)
        {
            await SendErrorAsync(
                "400_INVALID_CHUNK_INDEX",
                $"Mảnh dữ liệu không đúng thứ tự. Máy chủ đang chờ mảnh {_expectedChunkIndex}.",
                cancellationToken);

            return;
        }

        if (request.TotalChunks > 0 &&
            _totalChunks > 0 &&
            request.TotalChunks != _totalChunks)
        {
            await SendErrorAsync(
                "400_INVALID_CHUNK_COUNT",
                "Tổng số mảnh không khớp với yêu cầu tải lên.",
                cancellationToken);

            return;
        }

        byte[] chunk;

        try
        {
            chunk = PacketHelper.DecodeBinaryData(
                request.DataBase64);
        }
        catch (FormatException)
        {
            await SendErrorAsync(
                "400_INVALID_DATA",
                "Dữ liệu mảnh không phải Base64 hợp lệ.",
                cancellationToken);

            return;
        }

        if (chunk.Length == 0)
        {
            await SendErrorAsync(
                "400_EMPTY_CHUNK",
                "Mảnh dữ liệu không được rỗng.",
                cancellationToken);

            return;
        }

        if (_receivedBytes + chunk.Length > _totalSize)
        {
            await SendErrorAsync(
                "400_SIZE_MISMATCH",
                "Dữ liệu nhận được vượt quá kích thước tệp đã khai báo.",
                cancellationToken);

            return;
        }

        try
        {
            await _fileStream.WriteAsync(
                chunk.AsMemory(),
                cancellationToken);

            _receivedBytes += chunk.Length;
            _expectedChunkIndex++;

            ServerLogger.LogInfo(
                $"Nhan chunk #{request.ChunkIndex} cua '{_fileName}', " +
                $"{chunk.Length} bytes, total={_receivedBytes}/{_totalSize}.");

            await SendPacketAsync(
                new ProtocolPacket
                {
                    Command = PacketCommand.UPLOAD_CHUNK,
                    Success = true,
                    FileName = _fileName,
                    ChunkIndex = request.ChunkIndex,
                    Message = "Nhận mảnh dữ liệu thành công."
                },
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            FileErrorInfo error =
                FileErrorHandler.FromException(
                    ex,
                    _fileName ?? "unknown",
                    _tempFilePath ?? "");

            await CleanupUploadAsync();

            await SendErrorAsync(
                error.ErrorCode,
                error.Message,
                cancellationToken);
        }
    }

    // ============================================================
    // UPLOAD_DONE
    // ============================================================

    public async Task HandleUploadDoneAsync(
        ProtocolPacket request,
        CancellationToken cancellationToken = default)
    {
        if (!_uploadStarted || _fileStream is null)
        {
            await SendErrorAsync(
                "400_UPLOAD_NOT_STARTED",
                "Chưa có phiên tải lên.",
                cancellationToken);

            return;
        }

        if (_uploadCompleted)
        {
            await SendErrorAsync(
                "409_UPLOAD_COMPLETED",
                "Phiên tải lên đã kết thúc.",
                cancellationToken);

            return;
        }

        if (_receivedBytes != _totalSize)
        {
            await SendErrorAsync(
                "400_SIZE_MISMATCH",
                $"Kích thước tệp không khớp. Đã nhận {_receivedBytes}/{_totalSize} byte.",
                cancellationToken);

            await CleanupUploadAsync();
            return;
        }

        if (_totalChunks > 0 &&
            _expectedChunkIndex != _totalChunks)
        {
            await SendErrorAsync(
                "400_CHUNK_COUNT_MISMATCH",
                $"Số mảnh không khớp. Đã nhận {_expectedChunkIndex}/{_totalChunks} mảnh.",
                cancellationToken);

            await CleanupUploadAsync();
            return;
        }

        string fileName = _fileName!;

        string finalPath =
            Path.Combine(_storagePath, fileName);

        try
        {
            await _fileStream.FlushAsync(cancellationToken);

            await _fileStream.DisposeAsync();
            _fileStream = null;

            if (_tempFilePath is null)
            {
                throw new IOException(
                    "Không xác định được tệp tạm.");
            }

            // Kiểm tra hash file sau khi ghi hoàn tất.
            bool hashValid =
                await FileIntegrityVerifier
                    .VerifyFileAndDeleteIfCorruptAsync(
                        _tempFilePath,
                        _expectedHash);

            if (!hashValid)
            {
                ServerLogger.LogWarning(
                    $"Hash khong khop khi upload '{fileName}'.");

                _tempFilePath = null;
                _uploadCompleted = true;

                await SendErrorAsync(
                    "400_HASH_MISMATCH",
                    "Tải tệp lên thất bại: SHA-256 không khớp.",
                    cancellationToken);

                ResetState();
                return;
            }

            // Kiểm tra lại tên file trước khi Move.
            string? fileNameError =
                PacketValidator.ValidateFileName(fileName);

            if (fileNameError != null)
            {
                await DeleteTempFileAsync();

                await SendErrorAsync(
                    fileNameError,
                    "Tên tệp không hợp lệ.",
                    cancellationToken);

                ResetState();
                return;
            }

            // Không ghi đè file đã tồn tại.
            if (File.Exists(finalPath))
            {
                await DeleteTempFileAsync();

                await SendErrorAsync(
                    "409_FILE_EXISTS",
                    $"Tệp '{fileName}' đã tồn tại trên máy chủ.",
                    cancellationToken);

                ResetState();
                return;
            }

            File.Move(
                _tempFilePath,
                finalPath);

            _tempFilePath = null;
            _uploadCompleted = true;

            ServerLogger.LogInfo(
                $"Upload thanh cong '{fileName}', size={_receivedBytes} bytes.");

            await SendPacketAsync(
                new ProtocolPacket
                {
                    Command = PacketCommand.UPLOAD_DONE,
                    Success = true,
                    FileName = fileName,
                    FileHash = _expectedHash,
                    TotalSize = _receivedBytes,
                    Message = "Tải tệp lên thành công."
                },
                cancellationToken);

            ResetState();
        }
        catch (OperationCanceledException)
        {
            await CleanupUploadAsync();
            throw;
        }
        catch (Exception ex)
        {
            FileErrorInfo error =
                FileErrorHandler.FromException(
                    ex,
                    fileName,
                    finalPath);

            await CleanupUploadAsync();

            await SendErrorAsync(
                error.ErrorCode,
                error.Message,
                cancellationToken);
        }
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private string CreateTempFilePath(string fileName)
    {
        string tempName =
            $"{fileName}.{Guid.NewGuid():N}.uploading";

        return Path.Combine(
            _storagePath,
            tempName);
    }

    private async Task DeleteTempFileAsync()
    {
        if (_fileStream is not null)
        {
            await _fileStream.DisposeAsync();
            _fileStream = null;
        }

        if (!string.IsNullOrWhiteSpace(_tempFilePath) &&
            File.Exists(_tempFilePath))
        {
            try
            {
                File.Delete(_tempFilePath);
            }
            catch
            {
                // Không để lỗi cleanup làm Server crash.
            }
        }

        _tempFilePath = null;
    }

    private async Task CleanupUploadAsync()
    {
        try
        {
            await DeleteTempFileAsync();
        }
        catch
        {
            // Cleanup không được làm Server crash.
        }

        ResetState();
    }

    private void ResetState()
    {
        _uploadStarted = false;
        _uploadCompleted = false;

        _fileName = null;
        _expectedHash = null;

        _totalSize = 0;
        _receivedBytes = 0;

        _expectedChunkIndex = 0;
        _totalChunks = 0;

        _tempFilePath = null;
    }

    private async Task SendPacketAsync(
        ProtocolPacket packet,
        CancellationToken cancellationToken)
    {
        byte[] data =
            PacketHelper.Encode(packet);

        await _stream.WriteAsync(
            data.AsMemory(),
            cancellationToken);

        await _stream.FlushAsync(
            cancellationToken);
    }

    private async Task SendErrorAsync(
        string errorCode,
        string message,
        CancellationToken cancellationToken)
    {
        await SendPacketAsync(
            new ProtocolPacket
            {
                Command = PacketCommand.ERROR_RESP,
                Success = false,
                ErrorCode = errorCode,
                Message = message
            },
            cancellationToken);
    }

    private static string GetUploadValidationMessage(
        string errorCode)
    {
        return errorCode switch
        {
            "400_INVALID_FILENAME" =>
                "Tên tệp không hợp lệ.",

            "413_FILE_TOO_LARGE" =>
                "Tệp vượt quá giới hạn 3 GB.",

            _ =>
                "Thông tin tải lên không hợp lệ."
        };
    }

    public void Dispose()
    {
        try
        {
            _fileStream?.Dispose();
            _fileStream = null;

            if (!string.IsNullOrWhiteSpace(_tempFilePath) &&
                File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }
        catch
        {
            // Không để lỗi Dispose làm Server crash.
        }

        ResetState();
    }
}