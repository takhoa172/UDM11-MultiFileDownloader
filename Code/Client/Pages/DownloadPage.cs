using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Client.Logic;
using Shared;

namespace Client.Pages
{
    public class DownloadPage : UserControl
    {
        private readonly NetworkService _networkService;
        private readonly BindingList<FileItem> _serverFiles;
        private readonly BindingList<FileItem> _downloadFiles;
        private readonly DownloadManager _downloadManager;
        private readonly List<DownloadHistoryEntry> _downloadHistory;

        private string _serverIp = "";
        private int _serverPort;
        private string _downloadFolder;
        private string _username;
        private string _sessionToken = "";
        private bool _isConnected;

        public event EventHandler? ConnectRequested;
        public event EventHandler? FolderChooseRequested;

        public BindingList<FileItem> ServerFiles => _serverFiles;
        public BindingList<FileItem> DownloadFiles => _downloadFiles;

        private GroupBox gbConnection;
        private Label lblStatus;
        private Button btnShowConnectDialog;
        private Label lblConnectedInfo;
        private Label lblSaveFolder;
        private TextBox txtSaveFolder;
        private Button btnChooseFolder;
        private Panel pnlServerToolbar;
        private Button btnRefreshServerList;
        private GroupBox gbServerFiles;
        private DataGridView dgvServer;
        private GroupBox gbDownloads;
        private DataGridView dgvDownload;
        private Label lblLastSaved;
        private Label lblDownloadStats;
        private TextBox txtNotification;

        public DownloadPage(
            NetworkService networkService,
            BindingList<FileItem> serverFiles,
            BindingList<FileItem> downloadFiles,
            DownloadManager downloadManager,
            List<DownloadHistoryEntry> downloadHistory,
            string downloadFolder)
        {
            _networkService = networkService;
            _serverFiles = serverFiles;
            _downloadFiles = downloadFiles;
            _downloadManager = downloadManager;
            _downloadHistory = downloadHistory;
            _downloadFolder = downloadFolder;

            InitializeComponent();
            SetupGridViews();
            SetupDragAndDrop();
            txtSaveFolder.Text = _downloadFolder;
        }

        public void SetConnectionState(
            bool isConnected,
            string serverIp,
            int serverPort,
            string username,
            string sessionToken = "")
        {
            _isConnected = isConnected;
            _serverIp = serverIp;
            _serverPort = serverPort;
            _username = username;
            _sessionToken = sessionToken;

            if (InvokeRequired)
            {
                BeginInvoke(() => UpdateConnectionUI());
                return;
            }
            UpdateConnectionUI();
        }

        private void UpdateConnectionUI()
        {
            if (_isConnected)
            {
                lblStatus.Text = "● Đã kết nối";
                lblStatus.ForeColor = Color.ForestGreen;
                btnShowConnectDialog.Visible = false;
                lblConnectedInfo.Visible = true;
                lblConnectedInfo.Text = $"Server: {_serverIp}:{_serverPort}";
            }
            else
            {
                lblStatus.Text = "● Chưa kết nối";
                lblStatus.ForeColor = Color.Red;
                btnShowConnectDialog.Visible = true;
                btnShowConnectDialog.Enabled = true;
                lblConnectedInfo.Visible = false;
                lblConnectedInfo.Text = "";
            }
        }

        public void SetNotification(string text, bool isError)
        {
            if (InvokeRequired) { BeginInvoke(() => SetNotification(text, isError)); return; }
            txtNotification.AppendText(text + Environment.NewLine);
            txtNotification.SelectionStart = txtNotification.TextLength;
            txtNotification.ScrollToCaret();
        }

        public void SetLastSaved(string path)
        {
            if (InvokeRequired) { BeginInvoke(() => SetLastSaved(path)); return; }
            lblLastSaved.Text = "Đã lưu: " + path;
        }

        public void UpdateStats()
        {
            if (InvokeRequired) { BeginInvoke(() => UpdateStats()); return; }
            int total = _downloadFiles.Count;
            int done = _downloadFiles.Count(f => f.Status == DownloadStatus.Completed);
            int err = _downloadFiles.Count(f => f.Status == DownloadStatus.Error);
            lblDownloadStats.Text = $"Tổng: {total} file | Đã tải: {done} | Lỗi: {err}";
        }

        public async Task FetchServerFileListAsync()
        {
            if (!_isConnected) return;

            try
            {
                ProtocolPacket response = await _networkService.RequestAsync(new ProtocolPacket { Command = PacketCommand.GET_LIST });

                if (response.Command == PacketCommand.ERROR_RESP) return;

                string rawData = PacketHelper.DecodeTextData(response.DataBase64);
                _serverFiles.Clear();

                if (string.IsNullOrWhiteSpace(rawData)) return;

                string[] lines = rawData.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    string[] parts = line.Split('|');
                    if (parts.Length < 3) continue;

                    string fileName = parts[0].Trim();
                    long.TryParse(parts[1], out long fileSize);
                    string fileHash = parts[2].Trim();

                    _serverFiles.Add(new FileItem
                    {
                        STT = _serverFiles.Count + 1,
                        FileName = fileName,
                        FileSizeBytes = fileSize,
                        FileHash = fileHash
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLIENT] Fetch loi: {ex.Message}");
            }
        }

        public void RestoreDownloads(List<DownloadState> savedStates, string downloadFolder)
        {
            _downloadFolder = downloadFolder;
            txtSaveFolder.Text = _downloadFolder;

            foreach (var state in savedStates)
            {
                int downloaded = state.FileSizeBytes > 0
                    ? (int)(state.DownloadedBytes * 100 / state.FileSizeBytes)
                    : 0;

                var item = new FileItem
                {
                    STT = _downloadFiles.Count + 1,
                    FileName = state.FileName,
                    DisplayName = state.DisplayName,
                    FileSizeBytes = state.FileSizeBytes,
                    DownloadedBytes = state.DownloadedBytes,
                    SavedPath = state.SavedPath,
                    Progress = downloaded,
                    Status = DownloadStatus.Downloading,
                    SpeedInfo = "Đang tiếp tục..."
                };

                _downloadFiles.Add(item);
                _ = StartDownloadAsync(item);
            }
            UpdateStats();
        }

        public void LoadDownloadHistory(string username)
        {
            _username = username;
            var history = DownloadHistoryStore.LoadByUsername(username);
            _downloadHistory.Clear();
            _downloadHistory.AddRange(history);
        }

        public async Task StartDownloadAsync(FileItem item)
        {
            if (item.Status == DownloadStatus.Completed) return;

            item.Status = DownloadStatus.Pending;
            item.SpeedInfo = "Chờ slot...";

            var progress = new Progress<DownloadProgressModel>(p =>
            {
                if (p.SpeedInfo == "Skipped") return;
                item.Progress = p.Percentage;
                item.SpeedInfo = p.SpeedInfo;
                item.DownloadedBytes = p.DownloadedBytes;
                if (item.Status == DownloadStatus.Pending)
                    item.Status = DownloadStatus.Downloading;
            });

            FileConflictMode conflictMode =
                Enum.TryParse(ClientConfig.Settings.Download.ConflictMode, true, out FileConflictMode parsedMode)
                    ? parsedMode
                    : FileConflictMode.AutoRename;

            DownloadResult result = await _downloadManager.StartDownloadAsync(
                item.FileName,
                item.FileSizeBytes,
                item.DownloadedBytes,
                item.SavedPath,
                _serverIp,
                _serverPort,
                _downloadFolder,
                _username,
                _sessionToken,
                progress,
                conflictMode);

            switch (result.Status)
            {
                case DownloadStatusResult.Completed:
                    item.Progress = 100;
                    item.SpeedInfo = "";
                    item.Status = DownloadStatus.Completed;
                    item.DisplayName = Path.GetFileName(result.SavedPath) ?? item.FileName;
                    SetLastSaved(result.SavedPath ?? "");
                    SetNotification($"Da tai xong: {Path.GetFileName(result.SavedPath)}", isError: false);
                    break;
                case DownloadStatusResult.Skipped:
                    item.Progress = 100;
                    item.SpeedInfo = "";
                    item.Status = DownloadStatus.Completed;
                    SetNotification($"Bo qua (da ton tai): {item.FileName}", isError: false);
                    break;
                default:
                    item.Progress = 0;
                    item.SpeedInfo = "";
                    item.Status = DownloadStatus.Error;
                    SetNotification($"Loi tai {item.FileName}: {result.Message}", isError: true);
                    break;
            }
            UpdateStats();
        }

        // ── UI Setup ──

        private void InitializeComponent()
        {
            this.SuspendLayout();

            gbConnection = new GroupBox();
            lblStatus = new Label();
            btnShowConnectDialog = new Button();
            lblConnectedInfo = new Label();
            lblSaveFolder = new Label();
            txtSaveFolder = new TextBox();
            btnChooseFolder = new Button();
            pnlServerToolbar = new Panel();
            btnRefreshServerList = new Button();
            gbServerFiles = new GroupBox();
            dgvServer = new DataGridView();
            gbDownloads = new GroupBox();
            dgvDownload = new DataGridView();
            lblLastSaved = new Label();
            lblDownloadStats = new Label();
            txtNotification = new TextBox();

            gbConnection.SuspendLayout();
            gbServerFiles.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvServer).BeginInit();
            gbDownloads.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvDownload).BeginInit();
            pnlServerToolbar.SuspendLayout();
            this.SuspendLayout();

            // gbConnection
            gbConnection.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            gbConnection.Controls.Add(lblStatus);
            gbConnection.Controls.Add(btnShowConnectDialog);
            gbConnection.Controls.Add(lblConnectedInfo);
            gbConnection.Controls.Add(lblSaveFolder);
            gbConnection.Controls.Add(txtSaveFolder);
            gbConnection.Controls.Add(btnChooseFolder);
            gbConnection.Location = new Point(12, 12);
            gbConnection.Size = new Size(980, 80);
            gbConnection.Text = "Ket noi Server";

            // lblStatus
            lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            lblStatus.ForeColor = Color.Red;
            lblStatus.Location = new Point(25, 30);
            lblStatus.Text = "Chua ket noi";

            // btnShowConnectDialog
            btnShowConnectDialog.Location = new Point(25, 50);
            btnShowConnectDialog.Size = new Size(130, 25);
            btnShowConnectDialog.Text = "Ket noi Server";
            btnShowConnectDialog.Click += (s, e) => ConnectRequested?.Invoke(this, EventArgs.Empty);

            // lblConnectedInfo
            lblConnectedInfo.AutoSize = true;
            lblConnectedInfo.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            lblConnectedInfo.ForeColor = Color.ForestGreen;
            lblConnectedInfo.Location = new Point(170, 30);
            lblConnectedInfo.Visible = false;

            // lblSaveFolder
            lblSaveFolder.AutoSize = true;
            lblSaveFolder.Location = new Point(450, 28);
            lblSaveFolder.Text = "Thu muc luu: ";

            // txtSaveFolder
            txtSaveFolder.Location = new Point(450, 45);
            txtSaveFolder.ReadOnly = true;
            txtSaveFolder.Size = new Size(430, 25);

            // btnChooseFolder
            btnChooseFolder.Location = new Point(886, 43);
            btnChooseFolder.Size = new Size(70, 27);
            btnChooseFolder.Text = "Chon...";
            btnChooseFolder.Click += (s, e) => FolderChooseRequested?.Invoke(this, EventArgs.Empty);

            // pnlServerToolbar
            pnlServerToolbar.Controls.Add(btnRefreshServerList);
            pnlServerToolbar.Dock = DockStyle.Top;
            pnlServerToolbar.Size = new Size(394, 35);

            // btnRefreshServerList
            btnRefreshServerList.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRefreshServerList.AutoSize = true;
            btnRefreshServerList.FlatStyle = FlatStyle.Flat;
            btnRefreshServerList.Location = new Point(300, 4);
            btnRefreshServerList.Size = new Size(80, 29);
            btnRefreshServerList.Text = "Lam moi";
            btnRefreshServerList.Click += async (s, e) => { await FetchServerFileListAsync(); };

            // gbServerFiles
            gbServerFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            gbServerFiles.Controls.Add(dgvServer);
            gbServerFiles.Controls.Add(pnlServerToolbar);
            gbServerFiles.Location = new Point(12, 98);
            gbServerFiles.Size = new Size(400, 550);
            gbServerFiles.Text = "Danh sach File tren Server";

            // dgvServer
            dgvServer.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvServer.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvServer.Dock = DockStyle.Fill;
            dgvServer.ReadOnly = true;
            dgvServer.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvServer.MultiSelect = true;

            // gbDownloads
            gbDownloads.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            gbDownloads.Controls.Add(dgvDownload);
            gbDownloads.Location = new Point(418, 98);
            gbDownloads.Size = new Size(574, 550);
            gbDownloads.Text = "Khu vuc Download (Keo tha file vao day)";

            // dgvDownload
            dgvDownload.AllowDrop = true;
            dgvDownload.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvDownload.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvDownload.Dock = DockStyle.Fill;
            dgvDownload.ReadOnly = true;
            dgvDownload.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // lblLastSaved
            lblLastSaved.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblLastSaved.AutoSize = true;
            lblLastSaved.ForeColor = Color.ForestGreen;
            lblLastSaved.Location = new Point(12, 658);
            lblLastSaved.Text = "Da luu: (chua co file nao)";

            // lblDownloadStats
            lblDownloadStats.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblDownloadStats.AutoSize = true;
            lblDownloadStats.ForeColor = Color.SteelBlue;
            lblDownloadStats.Location = new Point(200, 658);
            lblDownloadStats.Text = "Tong: 0 file | Da tai: 0 | Loi: 0";

            // txtNotification
            txtNotification.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtNotification.BackColor = Color.White;
            txtNotification.ForeColor = Color.ForestGreen;
            txtNotification.Location = new Point(12, 682);
            txtNotification.Multiline = true;
            txtNotification.ReadOnly = true;
            txtNotification.ScrollBars = ScrollBars.Vertical;
            txtNotification.Size = new Size(980, 70);

            this.Controls.Add(gbConnection);
            this.Controls.Add(gbServerFiles);
            this.Controls.Add(gbDownloads);
            this.Controls.Add(lblLastSaved);
            this.Controls.Add(lblDownloadStats);
            this.Controls.Add(txtNotification);

            ((System.ComponentModel.ISupportInitialize)dgvServer).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvDownload).EndInit();
            gbServerFiles.ResumeLayout(false);
            gbDownloads.ResumeLayout(false);
            pnlServerToolbar.ResumeLayout(false);
            pnlServerToolbar.PerformLayout();
            gbConnection.ResumeLayout(false);
            gbConnection.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void SetupGridViews()
        {
            dgvServer.DataSource = _serverFiles;
            dgvDownload.DataSource = _downloadFiles;

            dgvServer.Columns["DisplayName"].Visible = false;
            dgvServer.Columns["FileSizeBytes"].HeaderText = "Kich Thuoc";
            dgvServer.Columns["FileName"].HeaderText = "Ten File";
            dgvServer.Columns["STT"].HeaderText = "STT";
            dgvServer.Columns["STT"].Width = 40;
            dgvServer.Columns["Progress"].Visible = false;
            dgvServer.Columns["ProgressText"].Visible = false;
            dgvServer.Columns["Status"].Visible = false;
            dgvServer.Columns["StatusText"].Visible = false;
            dgvServer.Columns["SpeedInfo"].Visible = false;
            dgvServer.Columns["DownloadedBytes"].Visible = false;
            dgvServer.Columns["SavedPath"].Visible = false;
            dgvServer.Columns["FileHash"].HeaderText = "SHA-256";

            dgvDownload.Columns["FileName"].Visible = false;
            var colDisplayName = dgvDownload.Columns["DisplayName"];
            if (colDisplayName != null) { colDisplayName.HeaderText = "Ten File"; colDisplayName.DisplayIndex = 1; }
            var colSize = dgvDownload.Columns["FormattedSize"];
            if (colSize != null) colSize.HeaderText = "Kich Thuoc";
            var colProgress = dgvDownload.Columns["ProgressText"];
            if (colProgress != null) { colProgress.HeaderText = "Tien trinh"; colProgress.Visible = true; }
            var colStatus = dgvDownload.Columns["StatusText"];
            if (colStatus != null) { colStatus.HeaderText = "Trang Thai"; colStatus.Visible = true; }
            var colSpeed = dgvDownload.Columns["SpeedInfo"];
            if (colSpeed != null) { colSpeed.HeaderText = "Toc do"; colSpeed.Visible = true; }
            var colStt = dgvDownload.Columns["STT"];
            if (colStt != null) { colStt.HeaderText = "STT"; colStt.DisplayIndex = 0; colStt.Width = 40; }
            dgvDownload.Columns["DownloadedBytes"].Visible = false;
            dgvDownload.Columns["SavedPath"].Visible = false;
            dgvDownload.Columns["FileSizeBytes"].Visible = false;
            dgvDownload.Columns["Progress"].Visible = false;
            dgvDownload.Columns["Status"].Visible = false;
            dgvDownload.Columns["FileHash"].Visible = false;

            dgvDownload.CellPainting -= DgvDownload_CellPainting;
            dgvDownload.CellPainting += DgvDownload_CellPainting;

            dgvServer.Leave += (s, e) => { dgvServer.CurrentCell = null; dgvServer.ClearSelection(); };
            dgvDownload.Leave += (s, e) => { dgvDownload.CurrentCell = null; dgvDownload.ClearSelection(); };

            dgvServer.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 220, 255);
            dgvServer.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvDownload.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 220, 255);
            dgvDownload.DefaultCellStyle.SelectionForeColor = Color.Black;

            typeof(Control)
                .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(dgvServer, true, null);
            typeof(Control)
                .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(dgvDownload, true, null);
        }

        private void SetupDragAndDrop()
        {
            dgvDownload.DragEnter += (s, e) =>
            {
                if (e.Data != null && e.Data.GetDataPresent(typeof(List<FileItem>)))
                    e.Effect = DragDropEffects.Copy;
                else
                    e.Effect = DragDropEffects.None;
            };

            dgvDownload.DragDrop += (s, e) =>
            {
                if (e.Data == null || !e.Data.GetDataPresent(typeof(List<FileItem>))) return;
                var items = e.Data.GetData(typeof(List<FileItem>)) as List<FileItem>;
                if (items == null || items.Count == 0) return;

                foreach (var item in items)
                {
                    var downloadItem = new FileItem
                    {
                        STT = _downloadFiles.Count + 1,
                        FileName = item.FileName,
                        DisplayName = GetUniqueDownloadName(item.FileName),
                        FileSizeBytes = item.FileSizeBytes,
                        SavedPath = Path.Combine(_downloadFolder, item.FileName) + ".part",
                        Progress = 0,
                        Status = DownloadStatus.Pending,
                        SpeedInfo = ""
                    };
                    _downloadFiles.Add(downloadItem);
                    _ = StartDownloadAsync(downloadItem);
                }
                UpdateStats();
            };
        }

        private string GetUniqueDownloadName(string fileName)
        {
            var used = new HashSet<string>(_downloadFiles.Select(f => f.DisplayName), StringComparer.OrdinalIgnoreCase);
            string name = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);
            string candidate = fileName;
            int n = 1;
            while (used.Contains(candidate) ||
                   File.Exists(Path.Combine(_downloadFolder, candidate)) ||
                   File.Exists(Path.Combine(_downloadFolder, candidate + ".part")))
            {
                candidate = $"{name}({n}){ext}";
                n++;
            }
            return candidate;
        }

        private void DgvDownload_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string? columnName = dgvDownload.Columns[e.ColumnIndex]?.Name;
            if (string.IsNullOrEmpty(columnName)) return;

            Font fontToUse = e.CellStyle?.Font ?? dgvDownload.Font;

            if (columnName == "ProgressText")
            {
                if (dgvDownload.Rows[e.RowIndex]?.DataBoundItem is FileItem item)
                {
                    var paintParts = e.PaintParts & ~DataGridViewPaintParts.Focus & ~DataGridViewPaintParts.ContentForeground;
                    e.Paint(e.ClipBounds, paintParts);

                    int percentage = Math.Clamp(item.Progress, 0, 100);
                    if (percentage > 0)
                    {
                        int fillWidth = (e.CellBounds.Width - 4) * percentage / 100;
                        Color progressColor = percentage == 100 ? Color.ForestGreen : Color.DodgerBlue;
                        var rect = new Rectangle(e.CellBounds.X + 2, e.CellBounds.Y + 2, fillWidth, e.CellBounds.Height - 5);
                        using var brush = new SolidBrush(progressColor);
                        e.Graphics?.FillRectangle(brush, rect);
                    }

                    var borderRect = new Rectangle(e.CellBounds.X + 2, e.CellBounds.Y + 2, e.CellBounds.Width - 5, e.CellBounds.Height - 5);
                    using var pen = new Pen(Color.LightGray);
                    e.Graphics?.DrawRectangle(pen, borderRect);

                    if (!string.IsNullOrEmpty(item.ProgressText) && e.Graphics != null)
                    {
                        Color textColor = percentage > 50 ? Color.White : Color.Black;
                        TextRenderer.DrawText(e.Graphics, item.ProgressText, fontToUse, e.CellBounds, textColor,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    }
                    e.Handled = true;
                }
            }
            else if (columnName == "StatusText")
            {
                if (dgvDownload.Rows[e.RowIndex]?.DataBoundItem is FileItem item)
                {
                    var paintParts = DataGridViewPaintParts.Background | DataGridViewPaintParts.Border;
                    e.Paint(e.ClipBounds, paintParts);

                    if (item.Status != DownloadStatus.Pending && !string.IsNullOrEmpty(item.StatusText) && e.Graphics != null)
                    {
                        (Color bg, Color fg) = item.Status switch
                        {
                            DownloadStatus.Completed => (Color.FromArgb(198, 239, 206), Color.FromArgb(0, 97, 0)),
                            DownloadStatus.Error => (Color.FromArgb(255, 199, 206), Color.FromArgb(156, 0, 6)),
                            DownloadStatus.Downloading => (Color.FromArgb(204, 229, 255), Color.FromArgb(0, 70, 140)),
                            _ => (Color.WhiteSmoke, Color.Gray)
                        };
                        using var bgBrush = new SolidBrush(bg);
                        e.Graphics.FillRectangle(bgBrush, e.CellBounds);
                        TextRenderer.DrawText(e.Graphics, item.StatusText, fontToUse, e.CellBounds, fg,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    }
                    e.Handled = true;
                }
            }
            else if (columnName == "SpeedInfo")
            {
                if (dgvDownload.Rows[e.RowIndex]?.DataBoundItem is FileItem item)
                {
                    var paintParts = DataGridViewPaintParts.Background | DataGridViewPaintParts.Border;
                    e.Paint(e.ClipBounds, paintParts);
                    if (e.Graphics != null)
                    {
                        string text = item.Status == DownloadStatus.Downloading && !string.IsNullOrEmpty(item.SpeedInfo)
                            ? item.SpeedInfo : "—";
                        Color color = item.Status == DownloadStatus.Downloading ? Color.DarkOrange : Color.Gray;
                        TextRenderer.DrawText(e.Graphics, text, fontToUse, e.CellBounds, color,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    }
                    e.Handled = true;
                }
            }
        }
    }
}
