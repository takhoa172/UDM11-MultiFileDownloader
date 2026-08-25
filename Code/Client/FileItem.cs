namespace Client
{
    public class FileItem
    {
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }

        public string FormattedSize => FormatSize(FileSizeBytes);

        private static string FormatSize(long bytes)
        {
            if (bytes <= 0) return "0 MB";

            double sizeInMb = (double)bytes / (1024 * 1024);

            if (sizeInMb < 0.1)
            {
                double sizeInKb = (double)bytes / 1024;
                return $"{sizeInKb:0.##} KB";
            }
            else if (sizeInMb >= 1024)
            {
                double sizeInGb = sizeInMb / 1024;
                return $"{sizeInGb:0.##} GB";
            }
            else
            {
                return $"{sizeInMb:0.##} MB";
            }
        }
    }
}