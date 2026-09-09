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
                    $"File '{fileName}' khong ton tai tren Server."),

            DirectoryNotFoundException =>
                new FileErrorInfo(
                    "404_NOT_FOUND",
                    $"Thu muc chua file '{fileName}' khong ton tai."),

            UnauthorizedAccessException =>
                new FileErrorInfo(
                    "403_FORBIDDEN",
                    $"Server khong co quyen truy cap file '{fileName}'."),

            IOException when !File.Exists(filePath) =>
                new FileErrorInfo(
                    "404_NOT_FOUND",
                    $"File '{fileName}' da bi xoa hoac khong con ton tai."),

            IOException ioException =>
                new FileErrorInfo(
                    "500_FILE_READ_ERROR",
                    $"Khong the doc file '{fileName}': {ioException.Message}"),

            _ =>
                new FileErrorInfo(
                    "500_FILE_DOWNLOAD_ERROR",
                    $"Loi xu ly file '{fileName}': {exception.Message}")
        };
    }
}