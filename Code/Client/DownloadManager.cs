using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Client;

namespace Client.Logic
{
    public class DownloadProgressModel
    {
        public string FileName { get; set; } = string.Empty;
        public int Percentage { get; set; }
        public string SpeedInfo { get; set; } = string.Empty;
    }

    public class DownloadManager
    {
        private readonly SemaphoreSlim _semaphore;

        public DownloadManager(int maxConcurrentDownloads = 3)
        {
            _semaphore = new SemaphoreSlim(maxConcurrentDownloads);
        }

        public async Task StartDownloadAsync(
            string fileName,
            long totalBytes,
            Stream networkStream,
            string saveDirectory,
            IProgress<DownloadProgressModel> progress,
            FileConflictMode conflictMode = FileConflictMode.AutoRename)
        {
            await _semaphore.WaitAsync();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                if (!Directory.Exists(saveDirectory))
                {
                    Directory.CreateDirectory(saveDirectory);
                }

                // STT 8:
                // Xác định đường dẫn lưu file theo 3 chế độ:
                // AutoRename / Overwrite / Skip.
                string? filePath =
                    FileConflictManager.ResolveTargetPath(
                        saveDirectory,
                        fileName,
                        conflictMode);

                // Skip: file đã tồn tại và người dùng chọn bỏ qua.
                if (filePath == null)
                {
                    Console.WriteLine(
                        $"[SKIP] Bỏ qua file: {fileName}");

                    progress?.Report(new DownloadProgressModel
                    {
                        FileName = fileName,
                        Percentage = 100,
                        SpeedInfo = "Skipped"
                    });

                    return;
                }

                // FileMode.Create:
                // - File chưa tồn tại → tạo file mới.
                // - File đã tồn tại + Overwrite → ghi đè file cũ.
                // - AutoRename → filePath đã là tên mới nên tạo file mới.
                using (FileStream fileStream =
                    new FileStream(
                        filePath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        bufferSize: 8192,
                        useAsync: true))
                {
                    byte[] buffer = new byte[8192];

                    int bytesRead;
                    long downloadedBytes = 0;

                    while ((bytesRead =
                        await networkStream.ReadAsync(
                            buffer,
                            0,
                            buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(
                            buffer,
                            0,
                            bytesRead);

                        downloadedBytes += bytesRead;

                        int percentage = totalBytes > 0
                            ? (int)(
                                (double)downloadedBytes /
                                totalBytes * 100)
                            : 100;

                        // Không cho phần trăm vượt quá 100.
                        if (percentage > 100)
                            percentage = 100;

                        double elapsedSeconds =
                            stopwatch.Elapsed.TotalSeconds;

                        double speedMBps =
                            elapsedSeconds > 0
                                ? (downloadedBytes / elapsedSeconds) /
                                  (1024 * 1024)
                                : 0;

                        progress?.Report(
                            new DownloadProgressModel
                            {
                                FileName =
                                    Path.GetFileName(filePath),

                                Percentage = percentage,

                                SpeedInfo =
                                    $"{Math.Round(speedMBps, 2)} MB/s"
                            });

                        if (downloadedBytes >= totalBytes)
                        {
                            break;
                        }
                    }
                }

                Console.WriteLine(
                    $"[THÀNH CÔNG] Đã lưu file tại: {filePath}");
            }
            catch (IOException ex)
            {
                Console.WriteLine(
                    $"[LỖI FILE] Lỗi khi tải file {fileName}: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine(
                    $"[LỖI QUYỀN] Không có quyền ghi file {fileName}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[LỖI] Lỗi khi tải file {fileName}: {ex.Message}");
            }
            finally
            {
                stopwatch.Stop();
                _semaphore.Release();
            }
        }
    }
}