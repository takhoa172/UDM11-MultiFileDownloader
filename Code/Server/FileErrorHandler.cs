namespace Server;

public sealed record FileErrorInfo(
    string ErrorCode,
    string Message);

public static class FileErrorHandler
{
    public static FileErrorInfo FromException(
        Exception exception,
        string fileName,
        string filePath)
    {
        return exception switch
        {
            FileNotFoundException =>
                new FileErrorInfo(
                    "404_NOT_FOUND",
                    $"Tệp '{fileName}' không tồn tại trên máy chủ."),

            DirectoryNotFoundException =>
                new FileErrorInfo(
                    "404_NOT_FOUND",
                    $"Thư mục chứa tệp '{fileName}' không tồn tại."),

            UnauthorizedAccessException =>
                new FileErrorInfo(
                    "403_FORBIDDEN",
                    $"Máy chủ không có quyền truy cập tệp '{fileName}'."),

            IOException when !File.Exists(filePath) =>
                new FileErrorInfo(
                    "404_NOT_FOUND",
                    $"Tệp '{fileName}' đã bị xóa hoặc không còn tồn tại."),

            IOException ioException =>
                new FileErrorInfo(
                    "500_FILE_READ_ERROR",
                    $"Không thể đọc tệp '{fileName}': {ioException.Message}"),

            _ =>
                new FileErrorInfo(
                    "500_FILE_DOWNLOAD_ERROR",
                    $"Lỗi xử lý tệp '{fileName}': {exception.Message}")
        };
    }
}