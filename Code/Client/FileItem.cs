using System.ComponentModel;
using System.Runtime.CompilerServices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Client
{
    public enum DownloadStatus
    {
        Pending,
        Downloading,
        Completed,
        Error
    }

    public class FileItem : INotifyPropertyChanged
    {
        public int STT { get; set; }

        private string _fileName = string.Empty;
        private string _displayName = string.Empty;
        private long _fileSizeBytes;
        private int _progress;
        private DownloadStatus _status = DownloadStatus.Pending;
        private string _speedInfo = string.Empty;

        public string FileName
        {
            get => _fileName;
            set { if (_fileName != value) { _fileName = value; OnPropertyChanged(); } }
        }

        public string DisplayName
        {
            get => _displayName;
            set { if (_displayName != value) { _displayName = value; OnPropertyChanged(); } }
        }

        public long FileSizeBytes
        {
            get => _fileSizeBytes;
            set { if (_fileSizeBytes != value) { _fileSizeBytes = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormattedSize)); } }
        }

        public string FileHash { get; set; } = string.Empty;

        public string FormattedSize => FormatSize(FileSizeBytes);

        public int Progress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ProgressText));
                }
            }
        }

        public DownloadStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        public string SpeedInfo
        {
            get => _speedInfo;
            set { if (_speedInfo != value) { _speedInfo = value; OnPropertyChanged(); } }
        }

        public string ProgressText => Progress > 0 ? $"{Progress}%" : "";

        public string StatusText => Status switch
        {
            DownloadStatus.Pending => "Chờ slot...",
            DownloadStatus.Downloading => "Đang tải",
            DownloadStatus.Completed => "Hoàn thành",
            DownloadStatus.Error => "Lỗi",
            _ => ""
        };

        private static string FormatSize(long bytes)
        {
            if (bytes <= 0) return "0 MB";
            double sizeInMb = (double)bytes / (1024 * 1024);
            if (sizeInMb < 0.1) return $"{((double)bytes / 1024):0.##} KB";
            if (sizeInMb >= 1024) return $"{(sizeInMb / 1024):0.##} GB";
            return $"{sizeInMb:0.##} MB";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}