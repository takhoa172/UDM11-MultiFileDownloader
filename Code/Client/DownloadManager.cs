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
        public long DownloadedBytes { get; set; }
    }

    public enum DownloadStatusResult { Completed, Skipped, Error }

    public class DownloadResult
    {
        public DownloadStatusResult Status { get; set; }
        public string? SavedPath { get; set; }
        public string? Message { get; set; }
    }

    public class DownloadManager
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly ClientSettings _settings;

        public DownloadManager(int legacyMainFormPlaceholder = 3)
        {
            _settings = ClientSettings.Load();

            // Ưu tiên đọc cấu hình từ SettingPage, giới hạn 1-5 file
            int maxConcurrent = Math.Clamp(_settings.Download.MaxConcurrentDownloads, 1, 5);
            _semaphore = new SemaphoreSlim(maxConcurrent);
        }

        public async Task<DownloadResult> StartDownloadAsync(
            string fileName,
            long totalBytes,
            long downloadedBytes,
            string savedPath,
            string serverIp,
            int serverPort,
            string saveDirectory,
            string username,
            string sessionToken,
            IProgress<DownloadProgressModel>? progress = null,
            FileConflictMode conflictMode = FileConflictMode.AutoRename,
            CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync();

            Stopwatch stopwatch = Stopwatch.StartNew();
            string? filePath = null;
            string? expectedHash = null;

            try
            {
                Directory.CreateDirectory(saveDirectory);
                filePath = FileConflictManager.ResolveTargetPath(saveDirectory, fileName, conflictMode);

                if (filePath == null)
                {
                    progress?.Report(new DownloadProgressModel { FileName = fileName, Percentage = 100, SpeedInfo = "Skipped" });
                    return new DownloadResult { Status = DownloadStatusResult.Skipped, Message = "File đã tồn tại (bỏ qua)." };
                }

                using var client = new TcpClient();
                await client.ConnectAsync(serverIp, serverPort, cancellationToken);

                using var stream = client.GetStream();
                using var reader = new StreamReader(stream);
                using var writer = new StreamWriter(stream) { AutoFlush = true };

                await writer.WriteLineAsync(PacketHelper.EncodeToString(new ProtocolPacket
                {
                    Command = PacketCommand.DOWNLOAD_REQ,
                    FileName = fileName,
                    Username = username,
                    Token = sessionToken,
                    RequestedRateBytesPerSecond = _settings.Network.RequestedRateMBps * 1024L * 1024
                }));

                long currentDownloadedBytes = downloadedBytes;
                long lastReportMs = 0;
                string? errorMessage = null;

                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true))
                {
                    while (true)
                    {
                        string? line = await reader.ReadLineAsync(cancellationToken);
                        if (line == null) throw new IOException("Server đóng kết nối.");

                        ProtocolPacket packet = PacketHelper.Decode(line);

                        if (packet.Command == PacketCommand.ERROR_RESP)
                        {
                            errorMessage = packet.Message ?? packet.ErrorCode ?? "Lỗi không xác định.";
                            break;
                        }

                        if (packet.Command != PacketCommand.FILE_CHUNK) continue;

                        if (packet.IsLastChunk && !string.IsNullOrEmpty(packet.FileHash))
                            expectedHash = packet.FileHash;

                        if (!string.IsNullOrEmpty(packet.DataBase64))
                        {
                            byte[] chunk = Convert.FromBase64String(packet.DataBase64);
                            await fileStream.WriteAsync(chunk.AsMemory(), cancellationToken);

                            currentDownloadedBytes += chunk.Length;
                            if (packet.TotalSize > 0) totalBytes = packet.TotalSize;

                            long now = stopwatch.ElapsedMilliseconds;
                            if (now - lastReportMs >= 200 || packet.IsLastChunk)
                            {
                                lastReportMs = now;
                                int percentage = totalBytes > 0 ? (int)((double)currentDownloadedBytes / totalBytes * 100) : 0;
                                if (percentage > 100) percentage = 100;

                                double elapsed = stopwatch.Elapsed.TotalSeconds;
                                double speedMBps = elapsed > 0 ? (currentDownloadedBytes / elapsed) / (1024 * 1024) : 0;

                                progress?.Report(new DownloadProgressModel
                                {
                                    FileName = Path.GetFileName(filePath),
                                    Percentage = percentage,
                                    SpeedInfo = $"{speedMBps:F2} MB/s",
                                    DownloadedBytes = currentDownloadedBytes
                                });
                            }
                        }
                        if (packet.IsLastChunk) break;
                    }
                }

                if (errorMessage != null)
                {
                    TryDeleteFile(filePath);
                    return new DownloadResult { Status = DownloadStatusResult.Error, Message = errorMessage };
                }

                bool hashOk = await FileIntegrityVerifier.VerifyFileAndDeleteIfCorruptAsync(filePath, expectedHash);
                if (!hashOk) return new DownloadResult { Status = DownloadStatusResult.Error, Message = "File hỏng: hash không khớp." };

                return new DownloadResult { Status = DownloadStatusResult.Completed, SavedPath = filePath };
            }
            catch (OperationCanceledException)
            {
                TryDeleteFile(filePath);
                return new DownloadResult { Status = DownloadStatusResult.Error, Message = "Đã hủy tải." };
            }
            catch (Exception ex)
            {
                TryDeleteFile(filePath);
                return new DownloadResult { Status = DownloadStatusResult.Error, Message = ex.Message };
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
            catch { }
        }
    }
}
