namespace Server;

public static class ServerConfig
{
    public static readonly RateLimiter DownloadLimiter =
        new RateLimiter(5 * 1024 * 1024, 256 * 1024);

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