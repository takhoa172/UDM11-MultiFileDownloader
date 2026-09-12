using System.Net.Sockets;
using System.Security.Cryptography;
using Shared;

namespace Server;

public static class FileStreamer
{
    private const int BufferSize = 64 * 1024;
    private const int ReadTimeoutMs = 30000;
    private const int WriteTimeoutMs = 30000;

    public static async Task StreamFileAsync(
        NetworkStream stream,
        string filePath,
        string fileName,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            await SendErrorAsync(
                stream,
                "404_NOT_FOUND",
                "Khong tim thay file yeu cau.",
                cancellationToken);

            return;
        }

        try
        {
            using FileStream fileStream = new(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                BufferSize,
                useAsync: true);

            using SHA256 sha256 = SHA256.Create();

            byte[] buffer = new byte[BufferSize];

            int bytesRead;
            bool hasSentChunk = false;

            while (true)
            {
                using CancellationTokenSource readCts =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken);

                readCts.CancelAfter(ReadTimeoutMs);

                bytesRead = await fileStream.ReadAsync(
                    buffer.AsMemory(0, buffer.Length),
                    readCts.Token);

                if (bytesRead == 0)
                    break;

                hasSentChunk = true;

                byte[] chunk = buffer[..bytesRead];

                sha256.TransformBlock(
                    chunk,
                    0,
                    chunk.Length,
                    null,
                    0);

                bool isLastChunk =
                    fileStream.Position >= fileStream.Length;

                string chunkBase64 =
                    PacketHelper.EncodeBinaryData(chunk);

                await SendPacketAsync(
                    stream,
                    new ProtocolPacket
                    {
                        Command = PacketCommand.FILE_CHUNK,
                        FileName = fileName,
                        DataBase64 = chunkBase64,
                        IsLastChunk = isLastChunk,
                        FileHash = isLastChunk
                            ? GetFinalHash(sha256)
                            : null
                    },
                    cancellationToken);

                if (isLastChunk)
                    break;
            }

            // File rỗng
            if (!hasSentChunk)
            {
                sha256.TransformFinalBlock(
                    Array.Empty<byte>(),
                    0,
                    0);

                await SendPacketAsync(
                    stream,
                    new ProtocolPacket
                    {
                        Command = PacketCommand.FILE_CHUNK,
                        FileName = fileName,
                        DataBase64 = string.Empty,
                        IsLastChunk = true,
                        FileHash = Convert.ToHexString(
                            sha256.Hash!).ToLowerInvariant()
                    },
                    cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout hoặc download bị hủy.
            // Không gửi thêm ERROR_RESP vì request đã bị hủy.
            return;
        }
        catch (Exception ex)
        {
            FileErrorInfo error =
                FileErrorHandler.FromException(
                    ex,
                    fileName,
                    filePath);

            await TrySendErrorAsync(
                stream,
                error.ErrorCode,
                error.Message);
        }
    }

    private static string GetFinalHash(SHA256 sha256)
    {
        sha256.TransformFinalBlock(
            Array.Empty<byte>(),
            0,
            0);

        return Convert.ToHexString(
            sha256.Hash!).ToLowerInvariant();
    }

    private static async Task SendPacketAsync(
        NetworkStream stream,
        ProtocolPacket packet,
        CancellationToken cancellationToken)
    {
        byte[] data = PacketHelper.Encode(packet);

        using CancellationTokenSource writeCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        writeCts.CancelAfter(WriteTimeoutMs);

        await stream.WriteAsync(
            data.AsMemory(),
            writeCts.Token);

        await stream.FlushAsync(
            writeCts.Token);
    }

    private static async Task SendErrorAsync(
        NetworkStream stream,
        string errorCode,
        string message,
        CancellationToken cancellationToken)
    {
        await SendPacketAsync(
            stream,
            new ProtocolPacket
            {
                Command = PacketCommand.ERROR_RESP,
                ErrorCode = errorCode,
                Message = message
            },
            cancellationToken);
    }

    private static async Task TrySendErrorAsync(
        NetworkStream stream,
        string errorCode,
        string message)
    {
        try
        {
            using CancellationTokenSource errorCts =
                new(TimeSpan.FromMilliseconds(WriteTimeoutMs));

            await SendErrorAsync(
                stream,
                errorCode,
                message,
                errorCts.Token);
        }
        catch
        {
            // Client có thể đã ngắt kết nối.
            // Không để lỗi gửi ERROR_RESP làm Server crash.
        }
    }
}