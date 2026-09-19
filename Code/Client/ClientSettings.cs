using System;
using System.IO;
using System.Text.Json;

namespace Client
{
    public sealed class ClientSettings
    {
        public DownloadSection Download { get; set; } = new();
        public NetworkSection Network { get; set; } = new();

        private static readonly string SettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

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
                    WriteIndented = true
                });
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Lỗi khi lưu ClientSettings: {ex.Message}");
            }
        }
    }

    public sealed class DownloadSection
    {
        public int MaxConcurrentDownloads { get; set; } = 3; // Giới hạn 1 đến 5 file đồng thời
        public string ConflictMode { get; set; } = "AutoRename";
        public string SaveFolder { get; set; } = "";
    }

    public sealed class NetworkSection
    {
        public int RequestedRateMBps { get; set; } = 5; // Tùy chọn tốc độ 1, 5, hoặc 10 MB/s
        public int ConnectTimeoutMs { get; set; } = 5000;
        public int ReadTimeoutMs { get; set; } = 30000;
        public int WriteTimeoutMs { get; set; } = 30000;
    }
}