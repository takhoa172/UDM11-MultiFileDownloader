using System.Net.Sockets;
using System.Security.Cryptography;
using Shared;

namespace Server;

public static class FileStreamer
{
    private const int BufferSize = 64 * 1024;

    public static async Task StreamFileAsync(
        NetworkStream stream,
        string filePath,
        string fileName)
    {
        if (!File.Exists(filePath))
        {
            await SendErrorAsync(
                stream,
                "404_NOT_FOUND",
                "Khong tim thay file yeu cau.");

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

            while ((bytesRead = await fileStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
            {
                hasSentChunk = true;

                byte[] chunk = buffer[..bytesRead];

                byte[] hashInput = chunk;
                sha256.TransformBlock(
                    hashInput,
                    0,
                    hashInput.Length,
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
                    });
            }

            // Trường hợp file rỗng.
            if (!hasSentChunk)
            {
                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

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
                    });
            }
        }
        catch (FileNotFoundException)
        {
            await SendErrorAsync(
                stream,
                "404_NOT_FOUND",
                "File khong con ton tai tren Server.");
        }
        catch (DirectoryNotFoundException)
        {
            await SendErrorAsync(
                stream,
                "404_NOT_FOUND",
                "Thu muc chua file khong con ton tai.");
        }
        catch (UnauthorizedAccessException)
        {
            await SendErrorAsync(
                stream,
                "403_FORBIDDEN",
                "Server khong co quyen doc file.");
        }
        catch (IOException ex)
        {
            await SendErrorAsync(
                stream,
                "500_FILE_READ_ERROR",
                $"Khong the doc file: {ex.Message}");
        }
    }

    private static string GetFinalHash(SHA256 sha256)
    {
        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        return Convert.ToHexString(
            sha256.Hash!).ToLowerInvariant();
    }

    private static async Task SendPacketAsync(
        NetworkStream stream,
        ProtocolPacket packet)
    {
        byte[] data = PacketHelper.Encode(packet);

        await stream.WriteAsync(data);
        await stream.FlushAsync();
    }

    private static async Task SendErrorAsync(
        NetworkStream stream,
        string errorCode,
        string message)
    {
        await SendPacketAsync(
            stream,
            new ProtocolPacket
            {
                Command = PacketCommand.ERROR_RESP,
                ErrorCode = errorCode,
                Message = message
            });
    }
}