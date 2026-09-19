using System.Text.Json;

namespace Client;

public sealed class ClientSettings
{
    public DownloadSection Download { get; set; } = new();
    public NetworkSection Network { get; set; } = new();

    private static readonly string SettingsPath =
        Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    public static ClientSettings Load()
    {
        if (!File.Exists(SettingsPath))
            return new ClientSettings();

        try
        {
            return JsonSerializer.Deserialize<ClientSettings>(
                       File.ReadAllText(SettingsPath),
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? new ClientSettings();
        }
        catch
        {
            return new ClientSettings();
        }
    }

    public void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
        }
    }
}

public sealed class DownloadSection
{
    public int MaxConcurrentDownloads { get; set; } = 3;
    public long SpeedLimitMBs { get; set; } = 5;
    public string ConflictMode { get; set; } = "AutoRename";
    public string SaveFolder { get; set; } = "";
}

public sealed class NetworkSection
{
    public int ConnectTimeoutMs { get; set; } = 5000;
    public int ReadTimeoutMs { get; set; } = 30000;
    public int WriteTimeoutMs { get; set; } = 30000;
}
