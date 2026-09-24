using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
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
                MessageBox.Show("Tệp chọn tải lên không tồn tại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false; // Hiện thông báo và dừng tải
            }

            //Hiển thị thông báo khi file > 3GB
            if (fileInfo.Length > MaxFileSize)
            {
                MessageBox.Show("Kích thước tệp vượt quá 3GB!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            int totalChunks = fileInfo.Length == 0
                ? 0
                : (int)Math.Ceiling((double)fileInfo.Length / BufferSize);

            ProtocolPacket ready = await networkService.RequestAsync(new ProtocolPacket
            {
                Command = PacketCommand.UPLOAD_REQ,
                FileName = fileInfo.Name,
                TotalSize = fileInfo.Length,
                TotalChunks = totalChunks
            });

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

                    ProtocolPacket chunkAck = await networkService.RequestAsync(new ProtocolPacket
                    {
                        Command = PacketCommand.UPLOAD_CHUNK,
                        FileName = actualName,
                        DataBase64 = PacketHelper.EncodeBinaryData(buffer[..bytesRead]),
                        ChunkIndex = chunkIndex++,
                        TotalChunks = totalChunks,
                        IsLastChunk = isLastChunk
                    });

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

            ProtocolPacket result = await networkService.RequestAsync(new ProtocolPacket
            {
                Command = PacketCommand.UPLOAD_DONE,
                FileName = actualName
            });

            if (result.Command == PacketCommand.ERROR_RESP || !result.Success)
            {
                return false;
            }

            progress?.Report(100);
            return true;
        }
    }
}