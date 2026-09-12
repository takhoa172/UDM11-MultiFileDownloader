namespace Server;

public static class ServerConfig
{
    public static readonly RateLimiter DownloadLimiter =
        new RateLimiter(5 * 1024 * 1024);

    public static string StoragePath { get; } = FindStoragePath();

    private static string FindStoragePath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null && !dir.GetFiles("*.csproj").Any())
        {
            dir = dir.Parent;
        }

        if (dir != null)
        {
            string storagePath = Path.Combine(dir.FullName, "Storage");

            if (!Directory.Exists(storagePath))
                Directory.CreateDirectory(storagePath);

            return storagePath;
        }

        string fallback = Path.Combine(AppContext.BaseDirectory, "Storage");
        Directory.CreateDirectory(fallback);
        return fallback;
    }
}