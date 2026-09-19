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
        public int Percentage { get; set; }
        public string SpeedInfo { get; set; } = string.Empty;
        public long DownloadedBytes { get; set; }
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
            long downloadedBytes,
            string existingPartPath,
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

                string? partPath = existingPartPath;
                bool isResume = false;

                if (!string.IsNullOrEmpty(partPath) && File.Exists(partPath))
                {
                    long partLength = new FileInfo(partPath).Length;
                    isResume = partLength > 0;
                    downloadedBytes = partLength;
                    filePath = Path.Combine(saveDirectory, fileName);
                }
                else
                {
                    filePath = FileConflictManager.ResolveTargetPath(
                        saveDirectory, fileName, conflictMode);

                    if (filePath == null)
                    {
                        progress?.Report(new DownloadProgressModel
                        {
                            Percentage = 100,
                            SpeedInfo = "Skipped"
                        });

                        return new DownloadResult
                        {
                            Status = DownloadStatusResult.Skipped,
                            Message = "File đã tồn tại (bỏ qua)."
                        };
                    }

                    partPath = filePath + ".part";
                    downloadedBytes = 0;
                    TryDeleteFile(partPath);
                }

                using var client = new TcpClient();
                await client.ConnectAsync(serverIp, serverPort);

                using var stream = client.GetStream();
                using var reader = new StreamReader(stream);
                using var writer = new StreamWriter(stream) { AutoFlush = true };

                await writer.WriteLineAsync(PacketHelper.EncodeToString(new ProtocolPacket
                {
                    Command = PacketCommand.DOWNLOAD_REQ,
                    FileName = fileName,
                    Offset = downloadedBytes
                }));

                long totalDownloaded = downloadedBytes;
                long lastReportMs = 0;
                string? errorMessage = null;

                FileMode mode = isResume ? FileMode.Append : FileMode.Create;

                using (var fileStream = new FileStream(
                    partPath,
                    mode,
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
                            errorMessage = packet.Message
                                           ?? packet.ErrorCode
                                           ?? "Lỗi không xác định.";
                            break;
                        }

                        if (packet.Command != PacketCommand.FILE_CHUNK)
                            continue;

                        if (packet.IsLastChunk && !string.IsNullOrEmpty(packet.FileHash))
                            expectedHash = packet.FileHash;

                        if (!string.IsNullOrEmpty(packet.DataBase64))
                        {
                            byte[] chunk = Convert.FromBase64String(packet.DataBase64);
                            await fileStream.WriteAsync(chunk, 0, chunk.Length);

                            totalDownloaded += chunk.Length;

                            if (packet.TotalSize > 0)
                                totalBytes = packet.TotalSize;

                            long now = stopwatch.ElapsedMilliseconds;
                            if (now - lastReportMs >= 200 || packet.IsLastChunk)
                            {
                                lastReportMs = now;

                                int percentage = totalBytes > 0
                                    ? (int)((double)totalDownloaded / totalBytes * 100)
                                    : 0;

                                if (percentage > 100)
                                    percentage = 100;

                                double elapsed = stopwatch.Elapsed.TotalSeconds;
                                double speedMBps = elapsed > 0
                                    ? (totalDownloaded / elapsed) / (1024 * 1024)
                                    : 0;

                                progress?.Report(new DownloadProgressModel
                                {
                                    Percentage = percentage,
                                    SpeedInfo = $"{speedMBps:F2} MB/s",
                                    DownloadedBytes = totalDownloaded
                                });
                            }
                        }

                        if (packet.IsLastChunk)
                            break;
                    }
                }

                if (errorMessage != null)
                {
                    TryDeleteFile(partPath);
                    return new DownloadResult
                    {
                        Status = DownloadStatusResult.Error,
                        Message = errorMessage
                    };
                }

                bool hashOk = await FileIntegrityVerifier
                    .VerifyFileAndDeleteIfCorruptAsync(partPath, expectedHash);

                if (!hashOk)
                {
                    TryDeleteFile(partPath);
                    return new DownloadResult
                    {
                        Status = DownloadStatusResult.Error,
                        Message = "File hỏng: hash không khớp."
                    };
                }

                File.Move(partPath, filePath);

                return new DownloadResult
                {
                    Status = DownloadStatusResult.Completed,
                    SavedPath = filePath
                };
            }
            catch (Exception ex)
            {
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
            }
        }
    }
}
