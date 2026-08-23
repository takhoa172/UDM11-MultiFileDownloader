using System;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Logic 
{
    public class DownloadManager
    {
        // Khai báo SemaphoreSlim để giới hạn số luồng
        private readonly SemaphoreSlim _semaphore;

        // Hàm khởi tạo (Constructor). Theo yêu cầu nhóm, giới hạn tối đa 3 file tải cùng lúc
        public DownloadManager(int maxConcurrentDownloads = 3)
        {
            _semaphore = new SemaphoreSlim(maxConcurrentDownloads);
        }

        // Hàm xử lý tải một file
        public async Task StartDownloadAsync(string fileName)
        {
            // 1. ĐỨNG CHỜ Ở CỔNG
            // Nếu đã có đủ 3 file đang tải, file thứ 4 sẽ phải đứng đợi (Queued) ngay tại dòng này.
            await _semaphore.WaitAsync();

            try
            {
                // 2. BẮT ĐẦU TẢI FILE
                // Ngay khi qua được cổng, file sẽ bắt đầu tải.
                Console.WriteLine($"[ĐANG TẢI] Bắt đầu tải file: {fileName}...");

                // Giả lập thời gian tải file 3 giây để test 
                await Task.Delay(3000);

                Console.WriteLine($"[THÀNH CÔNG] Đã tải xong: {fileName}");
            }
            catch (Exception ex)
            {
                // Task 21: Cách ly ngoại lệ. Lỗi file này không ảnh hưởng file khác
                Console.WriteLine($"[LỖI] Lỗi khi tải file {fileName}: {ex.Message}");
            }
            finally
            {
                // 3. TRẢ LẠI CHỖ TRỐNG
                // Dù file tải thành công hay bị lỗi văng vào catch, khối finally luôn chạy.
                // Hàm Release() sẽ mở cổng để file thứ 4 đang chờ được phép chạy tiếp.
                _semaphore.Release();
            }
        }
    }
}
