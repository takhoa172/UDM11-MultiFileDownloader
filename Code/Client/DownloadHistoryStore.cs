using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Client
{
    public static class DownloadHistoryStore
    {
        private static readonly string HistoryFilePath =
            Path.Combine(AppContext.BaseDirectory, "download_history.json");

        private static readonly object _lock = new();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public static void Save(List<DownloadHistoryEntry> entries)
        {
            lock (_lock)
            {
                try
                {
                    string json = JsonSerializer.Serialize(entries, JsonOptions);
                    File.WriteAllText(HistoryFilePath, json);
                }
                catch
                {
                }
            }
        }

        public static List<DownloadHistoryEntry> Load()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(HistoryFilePath))
                        return new List<DownloadHistoryEntry>();

                    string json = File.ReadAllText(HistoryFilePath);
                    return JsonSerializer.Deserialize<List<DownloadHistoryEntry>>(json, JsonOptions)
                           ?? new List<DownloadHistoryEntry>();
                }
                catch
                {
                    return new List<DownloadHistoryEntry>();
                }
            }
        }

        public static void AddEntry(DownloadHistoryEntry entry)
        {
            lock (_lock)
            {
                try
                {
                    var entries = Load();
                    entries.Add(entry);
                    Save(entries);
                }
                catch
                {
                }
            }
        }

        public static void RemoveEntry(string savedPath)
        {
            lock (_lock)
            {
                try
                {
                    var entries = Load();
                    entries.RemoveAll(e =>
                        string.Equals(e.SavedPath, savedPath, StringComparison.OrdinalIgnoreCase));
                    Save(entries);
                }
                catch
                {
                }
            }
        }

        public static List<DownloadHistoryEntry> LoadByUsername(string username)
        {
            return Load()
                .Where(e => string.Equals(e.Username, username, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}
