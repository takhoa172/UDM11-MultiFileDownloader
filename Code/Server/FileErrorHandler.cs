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
                    $"File '{fileName}' không tồn tại trên Server."),

            DirectoryNotFoundException =>
                new FileErrorInfo(
                    "404_NOT_FOUND",
                    $"Thư mục chứa file '{fileName}' không tồn tại."),

            UnauthorizedAccessException =>
                new FileErrorInfo(
                    "403_FORBIDDEN",
                    $"Server không có quyền truy cập file '{fileName}'."),

            IOException when !File.Exists(filePath) =>
                new FileErrorInfo(
                    "404_NOT_FOUND",
                    $"File '{fileName}' đã bị xóa hoặc không còn tồn tại."),

            IOException ioException =>
                new FileErrorInfo(
                    "500_FILE_READ_ERROR",
                    $"Không thể đọc file '{fileName}': {ioException.Message}"),

            _ =>
                new FileErrorInfo(
                    "500_FILE_DOWNLOAD_ERROR",
                    $"Lỗi xử lý file '{fileName}': {exception.Message}")
        };
    }
}