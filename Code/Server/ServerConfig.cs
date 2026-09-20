namespace Server;

public static class ServerConfig
{
    public static ServerSettings Settings { get; } = ServerSettings.Load();

    public static int Port => Settings.Server.Port;
    public static int ReadTimeoutMs => Settings.Timeout.ReadMs;
    public static int WriteTimeoutMs => Settings.Timeout.WriteMs;
    public static int BufferSize => Settings.Transfer.BufferSize;
    public static long TotalBytesPerSecond => Settings.RateLimit.TotalBytesPerSecond;

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
