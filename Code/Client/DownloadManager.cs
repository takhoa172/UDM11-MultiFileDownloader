using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Client;
using Shared;

namespace Client.Logic
{
    public class DownloadProgressModel
    {
        public string FileName { get; set; } = string.Empty;
        public int Percentage { get; set; }
        public string SpeedInfo { get; set; } = string.Empty;
    }

    public enum DownloadStatusResult
    {
        Completed,
        Skipped,
        Error
    }

    public class DownloadResult
    {
        public DownloadStatusResult Status { get; set; }
        public string? SavedPath { get; set; }
        public string? Message { get; set; }
    }

    public class DownloadManager
    {
        private readonly SemaphoreSlim _semaphore;

        public DownloadManager(int maxConcurrentDownloads = 3)
        {
            _semaphore = new SemaphoreSlim(maxConcurrentDownloads);
        }

        public async Task<DownloadResult> StartDownloadAsync(
            string fileName,
            long totalBytes,
            string serverIp,
            int serverPort,
            string saveDirectory,
            IProgress<DownloadProgressModel>? progress = null,
            FileConflictMode conflictMode = FileConflictMode.AutoRename)
        {
            await _semaphore.WaitAsync();

            Stopwatch stopwatch = Stopwatch.StartNew();

            string? filePath = null;
            string? expectedHash = null;

            try
            {
                Directory.CreateDirectory(saveDirectory);

                // STT 8: xác định đường dẫn lưu theo 3 chế độ
                // AutoRename / Overwrite / Skip.
                filePath = FileConflictManager.ResolveTargetPath(
                    saveDirectory, fileName, conflictMode);

                if (filePath == null)
                {
                    progress?.Report(new DownloadProgressModel
                    {
                        FileName = fileName,
                        Percentage = 100,
                        SpeedInfo = "Skipped"
                    });

                    return new DownloadResult
                    {
                        Status = DownloadStatusResult.Skipped,
                        Message = "File đã tồn tại (bỏ qua)."
                    };
                }

                using var client = new TcpClient();
                await client.ConnectAsync(serverIp, serverPort);

                using var stream = client.GetStream();
                using var reader = new StreamReader(stream);
                using var writer = new StreamWriter(stream) { AutoFlush = true };

                await writer.WriteLineAsync(PacketHelper.EncodeToString(new ProtocolPacket
                {
                    Command = PacketCommand.DOWNLOAD_REQ,
                    FileName = fileName
                }));

                long downloadedBytes = 0;
                long lastReportMs = 0;
                string? errorMessage = null;

                // STT 19: ghi file theo luồng (FileStream) để tránh tràn RAM.
                using (var fileStream = new FileStream(
                    filePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 8192,
                    useAsync: true))
                {
                    while (true)
                    {
                        string? line = await reader.ReadLineAsync();
                        if (line == null)
                            throw new IOException("Server đóng kết nối.");

                        ProtocolPacket packet = PacketHelper.Decode(line);

                        if (packet.Command == PacketCommand.ERROR_RESP)
                        {
                            // Ghi nhận lỗi rồi thoát vòng lặp để đóng FileStream
                            // trước khi xóa file rỗng.
                            errorMessage = packet.Message
                                           ?? packet.ErrorCode
                                           ?? "Lỗi không xác định.";
                            break;
                        }

                        if (packet.Command != PacketCommand.FILE_CHUNK)
                            continue;

                        // STT 23: lấy hash server gửi ở chunk cuối (kể cả file rỗng).
                        if (packet.IsLastChunk && !string.IsNullOrEmpty(packet.FileHash))
                            expectedHash = packet.FileHash;

                        if (!string.IsNullOrEmpty(packet.DataBase64))
                        {
                            byte[] chunk = Convert.FromBase64String(packet.DataBase64);
                            await fileStream.WriteAsync(chunk, 0, chunk.Length);

                            downloadedBytes += chunk.Length;

                            if (packet.TotalSize > 0)
                                totalBytes = packet.TotalSize;

                            // STT 18: tính % và tốc độ.
                            long now = stopwatch.ElapsedMilliseconds;
                            if (now - lastReportMs >= 200 || packet.IsLastChunk)
                            {
                                lastReportMs = now;

                                int percentage = totalBytes > 0
                                    ? (int)((double)downloadedBytes / totalBytes * 100)
                                    : 0;

                                if (percentage > 100)
                                    percentage = 100;

                                double elapsed = stopwatch.Elapsed.TotalSeconds;
                                double speedMBps = elapsed > 0
                                    ? (downloadedBytes / elapsed) / (1024 * 1024)
                                    : 0;

                                progress?.Report(new DownloadProgressModel
                                {
                                    FileName = Path.GetFileName(filePath),
                                    Percentage = percentage,
                                    SpeedInfo = $"{speedMBps:F2} MB/s"
                                });
                            }
                        }

                        if (packet.IsLastChunk)
                            break;
                    }
                }

                // Server báo lỗi: xóa file rỗng/dở rồi trả lỗi.
                if (errorMessage != null)
                {
                    TryDeleteFile(filePath);
                    return new DownloadResult
                    {
                        Status = DownloadStatusResult.Error,
                        Message = errorMessage
                    };
                }

                // STT 23: xác thực SHA-256; sai thì tự xóa file + báo hỏng.
                bool hashOk = await FileIntegrityVerifier
                    .VerifyFileAndDeleteIfCorruptAsync(filePath, expectedHash);

                if (!hashOk)
                {
                    return new DownloadResult
                    {
                        Status = DownloadStatusResult.Error,
                        Message = "File hỏng: hash không khớp."
                    };
                }

                return new DownloadResult
                {
                    Status = DownloadStatusResult.Completed,
                    SavedPath = filePath
                };
            }
            // STT 21: cách ly ngoại lệ — lỗi file này không ảnh hưởng file khác.
            catch (Exception ex)
            {
                TryDeleteFile(filePath);
                return new DownloadResult
                {
                    Status = DownloadStatusResult.Error,
                    Message = ex.Message
                };
            }
            finally
            {
                stopwatch.Stop();
                _semaphore.Release();
            }
        }

        private static void TryDeleteFile(string? path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Bỏ qua: không để lỗi xóa file làm hỏng luồng.
            }
        }
    }
}
