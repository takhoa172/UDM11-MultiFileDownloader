using System.Text.Json;

namespace Server;

public sealed class ServerSettings
{
    public ServerSection Server { get; set; } = new();
    public RateLimitSection RateLimit { get; set; } = new();
    public TimeoutSection Timeout { get; set; } = new();
    public TransferSection Transfer { get; set; } = new();

    public static ServerSettings Load()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        if (!File.Exists(path))
            return new ServerSettings();

        try
        {
            return JsonSerializer.Deserialize<ServerSettings>(
                       File.ReadAllText(path),
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? new ServerSettings();
        }
        catch
        {
            // File cau hinh loi -> dung gia tri mac dinh.
            return new ServerSettings();
        }
    }
}

public sealed class ServerSection
{
    public int Port { get; set; } = 8080;
}

public sealed class RateLimitSection
{
    public long BytesPerSecond { get; set; } = 5 * 1024 * 1024;
    public long MaxBurstBytes { get; set; } = 256 * 1024;
}

public sealed class TimeoutSection
{
    public int ReadMs { get; set; } = 30000;
    public int WriteMs { get; set; } = 30000;
}

public sealed class TransferSection
{
    public int BufferSize { get; set; } = 64 * 1024;
}
