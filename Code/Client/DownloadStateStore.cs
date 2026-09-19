using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Client
{
    public static class DownloadStateStore
    {
        private static readonly string StateFilePath =
            Path.Combine(AppContext.BaseDirectory, "download_state.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public static void Save(List<DownloadState> states)
        {
            try
            {
                string json = JsonSerializer.Serialize(states, JsonOptions);
                File.WriteAllText(StateFilePath, json);
            }
            catch
            {
            }
        }

        public static List<DownloadState> Load()
        {
            try
            {
                if (!File.Exists(StateFilePath))
                    return new List<DownloadState>();

                string json = File.ReadAllText(StateFilePath);
                return JsonSerializer.Deserialize<List<DownloadState>>(json, JsonOptions)
                       ?? new List<DownloadState>();
            }
            catch
            {
                return new List<DownloadState>();
            }
        }

        public static void Clear()
        {
            try
            {
                if (File.Exists(StateFilePath))
                    File.Delete(StateFilePath);
            }
            catch
            {
            }
        }
    }
}
