using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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

        public async Task StartDownloadAsync(string fileName, long totalBytes, Stream networkStream, string saveDirectory, IProgress<DownloadProgressModel> progress)
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

                // TASK 20: LOGIC XỬ LÝ FILE TRÙNG (TỰ ĐỘNG ĐỔI TÊN)
                string filePath = Path.Combine(saveDirectory, fileName);
                if (File.Exists(filePath))
                {
                    string fileExtension = Path.GetExtension(fileName); // VD: .pdf
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName); // VD: tailieu
                    int counter = 1;

                    // Lặp cho đến khi tìm được tên file chưa tồn tại (VD: tailieu(1).pdf)
                    while (File.Exists(filePath))
                    {
                        string newFileName = $"{fileNameWithoutExt}({counter}){fileExtension}";
                        filePath = Path.Combine(saveDirectory, newFileName);
                        counter++;
                    }
                }

                // TASK 19: Ghi file bằng FileStream
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true))
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;
                    long downloadedBytes = 0;

                    while ((bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        downloadedBytes += bytesRead;

                        // TASK 18: Tính % và tốc độ
                        int percentage = (int)((double)downloadedBytes / totalBytes * 100);
                        double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                        double speedMBps = elapsedSeconds > 0 ? (downloadedBytes / elapsedSeconds) / (1024 * 1024) : 0;

                        progress?.Report(new DownloadProgressModel
                        {
                            FileName = Path.GetFileName(filePath), // Lấy tên file thực tế (có thể đã đổi thành tailieu(1).pdf)
                            Percentage = percentage,
                            SpeedInfo = $"{Math.Round(speedMBps, 2)} MB/s"
                        });

                        if (downloadedBytes >= totalBytes)
                        {
                            break;
                        }
                    }
                }
                Console.WriteLine($"[THÀNH CÔNG] Đã lưu file tại: {filePath}");
            }
            catch (Exception ex)
            {
                // TASK 21: Cách ly ngoại lệ
                Console.WriteLine($"[LỖI] Lỗi khi tải file {fileName}: {ex.Message}");
            }
            finally
            {
                stopwatch.Stop();
                _semaphore.Release();
            }
        }
    }
}