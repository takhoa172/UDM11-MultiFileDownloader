using System.Text.Json;

namespace Client;

public sealed class ClientSettings
{
    public DownloadSection Download { get; set; } = new();
    public NetworkSection Network { get; set; } = new();

    public static ClientSettings Load()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        if (!File.Exists(path))
            return new ClientSettings();

        try
        {
            return JsonSerializer.Deserialize<ClientSettings>(
                       File.ReadAllText(path),
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? new ClientSettings();
        }
        catch
        {
            return new ClientSettings();
        }
    }
}

public sealed class DownloadSection
{
    public int MaxConcurrentDownloads { get; set; } = 3;
    public string ConflictMode { get; set; } = "AutoRename";
    public string SaveFolder { get; set; } = "";
}

public sealed class NetworkSection
{
    public int ConnectTimeoutMs { get; set; } = 5000;
    public int ReadTimeoutMs { get; set; } = 30000;
    public int WriteTimeoutMs { get; set; } = 30000;
}
