namespace Client
{
    public class DownloadState
    {
        public string FileName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public long FileSizeBytes { get; set; }
        public long DownloadedBytes { get; set; }
        public string SavedPath { get; set; } = "";
    }
}
