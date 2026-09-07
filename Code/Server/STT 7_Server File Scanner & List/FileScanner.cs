using System.Security.Cryptography;

namespace Server;

public sealed class ServerFileInfo
{
    public string FileName { get; init; } = "";
    public long FileSize { get; init; }
    public string FileHash { get; init; } = "";
}

public static class FileScanner
{
    public static List<ServerFileInfo> Scan(string folderPath)
    {
        Directory.CreateDirectory(folderPath);

        List<ServerFileInfo> result = new();

        foreach (string filePath in Directory.EnumerateFiles(folderPath))
        {
            try
            {
                FileInfo fileInfo = new(filePath);

                string hash = CalculateSha256(filePath);

                result.Add(new ServerFileInfo
                {
                    FileName = fileInfo.Name,
                    FileSize = fileInfo.Length,
                    FileHash = hash
                });
            }
            catch (IOException)
            {
                // File đang bị sử dụng hoặc không thể đọc.
                // Bỏ qua file này để không làm Server bị crash.
            }
            catch (UnauthorizedAccessException)
            {
                // Không có quyền đọc file.
                // Bỏ qua file này.
            }
        }

        return result
            .OrderBy(x => x.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string CalculateSha256(string filePath)
    {
        using FileStream stream = File.OpenRead(filePath);
        using SHA256 sha256 = SHA256.Create();

        byte[] hash = sha256.ComputeHash(stream);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}