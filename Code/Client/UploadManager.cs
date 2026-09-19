using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Shared; 

namespace Client.Logic
{
    public class UploadManager
    {
        private const long MaxFileSize = 3L * 1024 * 1024 * 1024; // Giới hạn tối đa 3GB

        public async Task UploadFileAsync(
            string filePath,
            string serverIp,
            int serverPort,
            IProgress<int>? progress = null)
        {
            FileInfo fileInfo = new FileInfo(filePath);

            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException("File chọn tải lên không tồn tại.");
            }

            // Chặn file > 3GB theo yêu cầu
            if (fileInfo.Length > MaxFileSize)
            {
                throw new InvalidOperationException("Kích thước file vượt quá giới hạn 3GB.");
            }

            using var client = new TcpClient();
            await client.ConnectAsync(serverIp, serverPort);

            using var stream = client.GetStream();
            using var writer = new StreamWriter(stream) { AutoFlush = true };

            // Gửi gói yêu cầu Upload (UPLOAD_REQ)
            await writer.WriteLineAsync(PacketHelper.EncodeToString(new ProtocolPacket
            {
                Command = PacketCommand.UPLOAD_REQ,
                FileName = fileInfo.Name,
                TotalSize = fileInfo.Length
            }));

            // Đọc file vật lý và gửi dữ liệu cắt nhỏ (UPLOAD_CHUNK)
            using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192, useAsync: true);
            byte[] buffer = new byte[8192];
            int bytesRead;
            long totalBytesRead = 0;

            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                totalBytesRead += bytesRead;
                bool isLastChunk = totalBytesRead == fileInfo.Length;

                string dataBase64 = Convert.ToBase64String(buffer, 0, bytesRead);

                await writer.WriteLineAsync(PacketHelper.EncodeToString(new ProtocolPacket
                {
                    Command = PacketCommand.UPLOAD_CHUNK,
                    FileName = fileInfo.Name,
                    DataBase64 = dataBase64,
                    IsLastChunk = isLastChunk
                }));

                int percentage = (int)((double)totalBytesRead / fileInfo.Length * 100);
                progress?.Report(percentage);
            }

            // Gửi tín hiệu hoàn tất (UPLOAD_DONE)
            await writer.WriteLineAsync(PacketHelper.EncodeToString(new ProtocolPacket
            {
                Command = PacketCommand.UPLOAD_DONE,
                FileName = fileInfo.Name
            }));
        }
    }
}