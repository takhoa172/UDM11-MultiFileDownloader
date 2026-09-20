using System;
using System.IO;
using System.Threading.Tasks;
using Shared;

namespace Client.Logic
{
    public class UploadManager
    {
        private const long MaxFileSize = 3L * 1024 * 1024 * 1024;
        private const int BufferSize = 8192;

        public async Task<bool> UploadFileAsync(
            string filePath,
            NetworkService networkService,
            IProgress<int>? progress = null)
        {
            FileInfo fileInfo = new FileInfo(filePath);

            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException("File chọn tải lên không tồn tại.");
            }

            if (fileInfo.Length > MaxFileSize)
            {
                throw new InvalidOperationException("Kích thước file vượt quá giới hạn 3GB.");
            }

            int totalChunks = fileInfo.Length == 0
                ? 0
                : (int)Math.Ceiling((double)fileInfo.Length / BufferSize);

            await networkService.SendPacketAsync(new ProtocolPacket
            {
                Command = PacketCommand.UPLOAD_REQ,
                FileName = fileInfo.Name,
                TotalSize = fileInfo.Length,
                TotalChunks = totalChunks
            });

            ProtocolPacket ready = await networkService.ReadPacketAsync();
            if (ready.Command == PacketCommand.ERROR_RESP || !ready.Success)
            {
                return false;
            }

            string actualName = string.IsNullOrEmpty(ready.FileName)
                ? fileInfo.Name
                : ready.FileName;

            using (FileStream fileStream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                BufferSize,
                useAsync: true))
            {
                byte[] buffer = new byte[BufferSize];
                int bytesRead;
                long totalBytesRead = 0;
                int chunkIndex = 0;

                while ((bytesRead = await fileStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
                {
                    totalBytesRead += bytesRead;
                    bool isLastChunk = totalBytesRead == fileInfo.Length;

                    await networkService.SendPacketAsync(new ProtocolPacket
                    {
                        Command = PacketCommand.UPLOAD_CHUNK,
                        FileName = actualName,
                        DataBase64 = PacketHelper.EncodeBinaryData(buffer[..bytesRead]),
                        ChunkIndex = chunkIndex++,
                        TotalChunks = totalChunks,
                        IsLastChunk = isLastChunk
                    });

                    ProtocolPacket chunkAck = await networkService.ReadPacketAsync();
                    if (chunkAck.Command == PacketCommand.ERROR_RESP || !chunkAck.Success)
                    {
                        return false;
                    }

                    if (fileInfo.Length > 0)
                    {
                        progress?.Report((int)((double)totalBytesRead / fileInfo.Length * 100));
                    }
                }
            }

            await networkService.SendPacketAsync(new ProtocolPacket
            {
                Command = PacketCommand.UPLOAD_DONE,
                FileName = actualName
            });

            ProtocolPacket result = await networkService.ReadPacketAsync();
            if (result.Command == PacketCommand.ERROR_RESP || !result.Success)
            {
                return false;
            }

            progress?.Report(100);
            return true;
        }
    }
}
