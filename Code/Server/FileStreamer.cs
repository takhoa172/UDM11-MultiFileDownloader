using System.Net.Sockets;
using System.Security.Cryptography;
using Shared;

namespace Server;

public static class FileStreamer
{
    public static async Task StreamFileAsync(
        NetworkStream stream,
        string filePath,
        string fileName,
        long offset,
        CancellationToken cancellationToken,
        RateLimiter? userLimiter = null)
    {
        if (!File.Exists(filePath))
        {
            await SendErrorAsync(
                stream,
                "404_NOT_FOUND",
                "Không tìm thấy file yêu cầu.",
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
                ServerConfig.BufferSize,
                useAsync: true);

            long totalSize = fileStream.Length;

            if (offset > 0)
            {
                if (offset >= totalSize)
                {
                    await SendErrorAsync(
                        stream,
                        "416_RANGE_NOT_SATISFIABLE",
                        "Offset vượt qua kích thước file.",
                        cancellationToken);
                    return;
                }

                fileStream.Seek(offset, SeekOrigin.Begin);
            }

            using SHA256 sha256 = SHA256.Create();

            ServerLogger.LogDownload(fileName, totalSize);

            byte[] buffer = new byte[ServerConfig.BufferSize];

            int bytesRead;
            bool hasSentChunk = false;

            while (true)
            {
                using CancellationTokenSource readCts =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken);

                readCts.CancelAfter(ServerConfig.ReadTimeoutMs);

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

                await (userLimiter ?? ServerConfig.DownloadLimiter).ThrottleAsync(chunk.Length);

                await SendPacketAsync(
                    stream,
                    new ProtocolPacket
                    {
                        Command = PacketCommand.FILE_CHUNK,
                        FileName = fileName,
                        DataBase64 = chunkBase64,
                        IsLastChunk = isLastChunk,
                        TotalSize = totalSize,
                        FileHash = isLastChunk
                            ? GetFinalHash(sha256)
                            : null
                    },
                    cancellationToken);

                if (isLastChunk)
                    break;
            }

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
                        TotalSize = totalSize,
                        FileHash = Convert.ToHexString(
                            sha256.Hash!).ToLowerInvariant()
                    },
                    cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
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

        writeCts.CancelAfter(ServerConfig.WriteTimeoutMs);

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
                new(TimeSpan.FromMilliseconds(ServerConfig.WriteTimeoutMs));

            await SendErrorAsync(
                stream,
                errorCode,
                message,
                errorCts.Token);
        }
        catch
        {
        }
    }
}
