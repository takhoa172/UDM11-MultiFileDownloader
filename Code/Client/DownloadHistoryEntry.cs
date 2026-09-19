using System;

namespace Client
{
    public class DownloadHistoryEntry
    {
        public string FileName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public long FileSizeBytes { get; set; }
        public string SavedPath { get; set; } = "";
        public DateTime DownloadedAt { get; set; } = DateTime.Now;
        public string Type { get; set; } = "Download";
        public string Username { get; set; } = "";

        public string FormattedSize
        {
            get
            {
                if (FileSizeBytes <= 0) return "0 MB";
                double sizeInMb = (double)FileSizeBytes / (1024 * 1024);
                if (sizeInMb < 0.1) return $"{((double)FileSizeBytes / 1024):0.##} KB";
                if (sizeInMb >= 1024) return $"{(sizeInMb / 1024):0.##} GB";
                return $"{sizeInMb:0.##} MB";
            }
        }

        public string FormattedDate => DownloadedAt.ToString("dd/MM/yyyy HH:mm");
    }
}
