using System.Collections.Concurrent;

namespace Server;

public static class ServerConfig
{
    public static ServerSettings Settings { get; } = ServerSettings.Load();

    public static int Port => Settings.Server.Port;
    public static int ReadTimeoutMs => Settings.Timeout.ReadMs;
    public static int WriteTimeoutMs => Settings.Timeout.WriteMs;
    public static int BufferSize => Settings.Transfer.BufferSize;
    public static long TotalBytesPerSecond => Settings.RateLimit.TotalBytesPerSecond;

    public static readonly RateLimiter DownloadLimiter =
        new RateLimiter(Settings.RateLimit.BytesPerSecond, Settings.RateLimit.MaxBurstBytes);

    private static readonly ConcurrentDictionary<string, RateLimiter> _userLimiters = new();

    public static RateLimiter GetUserLimiter(string username, long speedMBs)
    {
        long bytesPerSecond = speedMBs * 1024 * 1024;
        return _userLimiters.AddOrUpdate(
            username,
            _ => new RateLimiter(bytesPerSecond, bytesPerSecond / 4),
            (_, _) => new RateLimiter(bytesPerSecond, bytesPerSecond / 4));
    }

    public static string ProjectRoot { get; } = FindProjectRoot();

    public static string StoragePath { get; } =
        EnsureDirectory(Path.Combine(ProjectRoot, "Storage"));

    public static string LogDirectory { get; } =
        EnsureDirectory(Path.Combine(ProjectRoot, "logs"));

    private static string FindProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !dir.GetFiles("*.csproj").Any())
            dir = dir.Parent;
        return dir?.FullName ?? AppContext.BaseDirectory;
    }

    private static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
