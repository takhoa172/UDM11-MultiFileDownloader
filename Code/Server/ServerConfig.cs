namespace Server;

public static class ServerConfig
{
    public static readonly RateLimiter DownloadLimiter =
        new RateLimiter(5 * 1024 * 1024);

    public static string StoragePath =>
        Path.Combine(AppContext.BaseDirectory, "Storage");
}