using Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using Client.Logic;

namespace Client
{
    public partial class MainForm : Form
    {
        public bool LogoutRequested { get; private set; }

        private bool _isConnected = false;
        private bool _isConnecting = false;
        private readonly NetworkService _networkService;
        private readonly System.Windows.Forms.Timer _heartbeatTimer = new();

        private readonly BindingList<FileItem> _serverFiles = new BindingList<FileItem>();
        private readonly BindingList<FileItem> _downloadFiles = new BindingList<FileItem>();
        private readonly List<FileItem> _uploadFiles = new List<FileItem>();
        private readonly List<DownloadHistoryEntry> _downloadHistory = new List<DownloadHistoryEntry>();
        private string _username = "";
        private string _sessionToken = "";
        private bool _isSavingSettings;
        private CancellationTokenSource _downloadCts = new();

        private DownloadManager _downloadManager =
            new DownloadManager(ClientConfig.Settings.Download.MaxConcurrentDownloads);
        private string _serverIp;
        private int _serverPort;

        private string _downloadFolder = ResolveSaveFolder();

        private static string ResolveSaveFolder()
        {
            string configured = ClientConfig.Settings.Download.SaveFolder;

            if (!string.IsNullOrWhiteSpace(configured))
                return configured;

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");
        }

        private bool _isSelecting = false;
        private Point _selectStartPoint;
        private Rectangle _selectionRect = Rectangle.Empty;

        private Point _dragStartPoint = Point.Empty;
        private bool _mouseDownOnEmpty = false;
        private bool _mouseDownForDrag = false;
        private List<FileItem> _savedSelection = new List<FileItem>();

        public MainForm(
            NetworkService networkService,
            string serverIp,
            int serverPort,
            string username,
            string sessionToken)
        {
            _networkService = networkService;
            _serverIp = serverIp;
            _serverPort = serverPort;
            _username = username;
            _sessionToken = sessionToken;

            InitializeComponent();

            SetupGridViews();
            SetupDragAndDrop();
            SetupManageFilePage();
            SetupSettingsPage();
            SwitchPage(pnlManageFilePage, btnNavManageFile);
            txtSaveFolder.Text = _downloadFolder;

            _heartbeatTimer.Interval = 10000;
            _heartbeatTimer.Tick += async (s, e) => await HeartbeatAsync();

            SetConnectionState(true);
            _heartbeatTimer.Start();
            LoadDownloadHistory();
            RefreshManageFileList();
            _ = FetchServerFileListAsync();
        }

        // ─────────────────────────────────────────────────────────────
        //  SETUP
        // ─────────────────────────────────────────────────────────────

        private void SetupGridViews()
        {
            dgvServer.DataSource = _serverFiles;
            dgvDownload.DataSource = _downloadFiles;

            dgvServer.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvDownload.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            dgvServer.BackgroundColor = Color.White;
            dgvDownload.BackgroundColor = Color.White;

            dgvServer.MultiSelect = true;
            dgvServer.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgvServer.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvDownload.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            dgvServer.AllowUserToResizeColumns = true;
            dgvDownload.AllowUserToResizeColumns = true;

            var colServerFileName = dgvServer.Columns["FileName"];
            if (colServerFileName != null)
            {
                colServerFileName.HeaderText = "Tên tệp";
                colServerFileName.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            var colServerDisplayName = dgvServer.Columns["DisplayName"];
            if (colServerDisplayName != null)
                colServerDisplayName.Visible = false;

            var colServerSize = dgvServer.Columns["FormattedSize"];
            if (colServerSize != null) colServerSize.HeaderText = "Kích Thước";

            var colServerHash = dgvServer.Columns["FileHash"];
            if (colServerHash != null)
            {
                colServerHash.HeaderText = "SHA-256";
                colServerHash.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                colServerHash.Width = 110;
            }

            var colDlFileName = dgvDownload.Columns["FileName"];
            if (colDlFileName != null) colDlFileName.Visible = false;

            var colDlDisplayName = dgvDownload.Columns["DisplayName"];
            if (colDlDisplayName != null)
            {
                colDlDisplayName.HeaderText = "Tên tệp";
                colDlDisplayName.DisplayIndex = 1;
                colDlDisplayName.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                colDlDisplayName.Width = 210;
            }

            var colDlSize = dgvDownload.Columns["FormattedSize"];
            if (colDlSize != null) colDlSize.HeaderText = "Kích Thước";

            var colDlProgress = dgvDownload.Columns["ProgressText"];
            if (colDlProgress != null)
            {
                colDlProgress.HeaderText = "Tiến trình";
                colDlProgress.Visible = true;
            }

            var colDlStatus = dgvDownload.Columns["StatusText"];
            if (colDlStatus != null)
            {
                colDlStatus.HeaderText = "Trạng Thái";
                colDlStatus.Visible = true;
            }

            var colDlSpeed = dgvDownload.Columns["SpeedInfo"];
            if (colDlSpeed != null)
            {
                colDlSpeed.HeaderText = "Tốc độ";
                colDlSpeed.Visible = true;
                colDlSpeed.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            var colServerStt = dgvServer.Columns["STT"];
            if (colServerStt != null)
            {
                colServerStt.HeaderText = "STT";
                colServerStt.DisplayIndex = 0;
                colServerStt.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                colServerStt.Width = 40;
                colServerStt.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            var colDlStt = dgvDownload.Columns["STT"];
            if (colDlStt != null)
            {
                colDlStt.HeaderText = "STT";
                colDlStt.DisplayIndex = 0;
                colDlStt.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                colDlStt.Width = 40;
                colDlStt.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            if (dgvServer.Columns["FileSizeBytes"] != null) dgvServer.Columns["FileSizeBytes"]!.Visible = false;
            if (dgvServer.Columns["Progress"] != null) dgvServer.Columns["Progress"]!.Visible = false;
            if (dgvServer.Columns["ProgressText"] != null) dgvServer.Columns["ProgressText"]!.Visible = false;
            if (dgvServer.Columns["Status"] != null) dgvServer.Columns["Status"]!.Visible = false;
            if (dgvServer.Columns["StatusText"] != null) dgvServer.Columns["StatusText"]!.Visible = false;
            if (dgvServer.Columns["SpeedInfo"] != null) dgvServer.Columns["SpeedInfo"]!.Visible = false;
            if (dgvServer.Columns["DownloadedBytes"] != null) dgvServer.Columns["DownloadedBytes"]!.Visible = false;
            if (dgvServer.Columns["SavedPath"] != null) dgvServer.Columns["SavedPath"]!.Visible = false;

            if (dgvDownload.Columns["DownloadedBytes"] != null) dgvDownload.Columns["DownloadedBytes"]!.Visible = false;
            if (dgvDownload.Columns["SavedPath"] != null) dgvDownload.Columns["SavedPath"]!.Visible = false;
            if (dgvDownload.Columns["FileSizeBytes"] != null) dgvDownload.Columns["FileSizeBytes"]!.Visible = false;
            if (dgvDownload.Columns["Progress"] != null) dgvDownload.Columns["Progress"]!.Visible = false;
            if (dgvDownload.Columns["Status"] != null) dgvDownload.Columns["Status"]!.Visible = false;
            if (dgvDownload.Columns["FileHash"] != null) dgvDownload.Columns["FileHash"]!.Visible = false;

            dgvDownload.CellPainting -= dgvDownload_CellPainting;
            dgvDownload.CellPainting += dgvDownload_CellPainting;

            dgvServer.Leave += (s, e) => { dgvServer.CurrentCell = null; dgvServer.ClearSelection(); };
            dgvDownload.Leave += (s, e) => { dgvDownload.CurrentCell = null; dgvDownload.ClearSelection(); };

            dgvServer.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 220, 255);
            dgvServer.DefaultCellStyle.SelectionForeColor = Color.Black;

            dgvDownload.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 220, 255);
            dgvDownload.DefaultCellStyle.SelectionForeColor = Color.Black;

            EnableDoubleBuffering(dgvServer);
            EnableDoubleBuffering(dgvDownload);

            dgvServer.MouseDown += dgvServer_Selection_MouseDown;
            dgvServer.MouseMove += dgvServer_Selection_MouseMove;
            dgvServer.MouseUp += dgvServer_Selection_MouseUp;
            dgvServer.Paint += dgvServer_Selection_Paint;
        }

        private void SetupDragAndDrop()
        {
            dgvDownload.DragEnter += dgvDownload_DragEnter;
            dgvDownload.DragDrop += dgvDownload_DragDrop;
        }

        // ─────────────────────────────────────────────────────────────
        //  SIDEBAR NAVIGATION
        // ─────────────────────────────────────────────────────────────

        private void SetupManageFilePage()
        {
            dgvManageFile.AutoGenerateColumns = false;

            _uploadProgressLabel.AutoSize = false;
            _uploadProgressLabel.TextAlign = ContentAlignment.MiddleLeft;
            _uploadProgressLabel.Location = new Point(580, 5);
            _uploadProgressLabel.Size = new Size(150, 18);
            _uploadProgressLabel.Visible = false;

            _uploadProgressBar.Minimum = 0;
            _uploadProgressBar.Maximum = 100;
            _uploadProgressBar.Location = new Point(730, 13);
            _uploadProgressBar.Size = new Size(150, 20);
            _uploadProgressBar.Visible = false;

            pnlManageFileToolbar.Controls.Add(_uploadProgressLabel);
            pnlManageFileToolbar.Controls.Add(_uploadProgressBar);

            dgvManageFile.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "STT",
                HeaderText = "STT",
                Width = 40,
                FillWeight = 30
            });
            dgvManageFile.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DisplayName",
                HeaderText = "Tên tệp",
                FillWeight = 40
            });
            dgvManageFile.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FormattedSize",
                HeaderText = "Kích Thước",
                FillWeight = 15
            });
            dgvManageFile.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "SavedPath",
                HeaderText = "Đường dẫn",
                FillWeight = 35
            });
            dgvManageFile.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DownloadedAt",
                HeaderText = "Ngày tải",
                FillWeight = 15
            });
            dgvManageFile.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Type",
                HeaderText = "Loại",
                FillWeight = 10
            });
        }

        private readonly ProgressBar _uploadProgressBar = new();
        private readonly Label _uploadProgressLabel = new();

        private void SetupSettingsPage()
        {
            btnChangePassword.Click += btnChangePassword_Click;
            _btnDisconnect.Click += btnDisconnect_Click;
            _btnSaveSettings.Click += btnSaveSettings_Click;
            _btnLogout.Click += btnLogout_Click;
        }

        private async void btnSaveSettings_Click(object? sender, EventArgs e)
        {
            if (_isSavingSettings)
                return;

            if (!TryReadServerEndpoint(out string ip, out int port))
                return;

            _isSavingSettings = true;
            if (_btnSaveSettings != null)
                _btnSaveSettings.Enabled = false;

            try
            {
                int maxConcurrent = (int)nudConcurrentDownloads.Value;
                int rateMBps = int.Parse(cmbSpeedLimit.SelectedItem?.ToString() ?? "5");

                ClientConfig.Settings.Download.MaxConcurrentDownloads = maxConcurrent;
                ClientConfig.Settings.Network.RequestedRateMBps = rateMBps;
                ClientConfig.Settings.Network.ServerIp = ip;
                ClientConfig.Settings.Network.ServerPort = port;
                ClientConfig.Settings.Save();

                _downloadManager = new DownloadManager(maxConcurrent);

                SetNotification($"Đã lưu số tệp tải đồng thời: {maxConcurrent} tệp", isError: false);

                if (!_isConnected)
                {
                    SetNotification(
                        $"Đã lưu tốc độ tải tối đa: {rateMBps} MB/s (áp dụng khi kết nối máy chủ).",
                        isError: false);
                }
                else
                {
                    await SendSpeedLimitAsync(rateMBps);
                }
            }
            finally
            {
                _isSavingSettings = false;
                if (_btnSaveSettings != null)
                    _btnSaveSettings.Enabled = true;
            }
        }

        private void btnDisconnect_Click(object? sender, EventArgs e)
        {
            _heartbeatTimer.Stop();
            CancelActiveDownloads();
            _networkService.Dispose();
            SetConnectionState(false);
            SetNotification("Đã ngắt kết nối.", isError: false);
        }

        private void CancelActiveDownloads()
        {
            try
            {
                _downloadCts.Cancel();
                _downloadCts.Dispose();
            }
            catch
            {
            }

            _downloadCts = new CancellationTokenSource();
        }

        private async void btnLogout_Click(object? sender, EventArgs e)
        {
            if (_btnLogout != null)
                _btnLogout.Enabled = false;

            _heartbeatTimer.Stop();

            try
            {
                if (_isConnected)
                {
                    await _networkService.RequestAsync(new ProtocolPacket
                    {
                        Command = PacketCommand.LOGOUT
                    });
                }
            }
            catch
            {
                // Closing the socket below still releases the session on Server.
            }
            finally
            {
                LogoutRequested = true;
                CancelActiveDownloads();
                _networkService.Dispose();
                Close();
            }
        }

        private Panel? _activePage;
        private Button? _activeNavButton;

        private void SwitchPage(Panel page, Button navButton)
        {
            if (_activePage != null) _activePage.Visible = false;
            if (_activeNavButton != null)
                _activeNavButton.BackColor = Color.FromArgb(30, 30, 60);

            page.Visible = true;
            page.BringToFront();
            navButton.BackColor = Color.FromArgb(50, 50, 90);

            _activePage = page;
            _activeNavButton = navButton;

            if (page == pnlManageFilePage)
                RefreshManageFileList();
        }

        // ─────────────────────────────────────────────────────────────
        //  SETTINGS PAGE
        // ─────────────────────────────────────────────────────────────

        private void LoadSettingsPage()
        {
            lblSettingsUsernameValue.Text = _username;
            _txtServerIp.Text = _isConnected ? _serverIp : ClientConfig.Settings.Network.ServerIp;
            _txtServerPort.Text = (_isConnected ? _serverPort : ClientConfig.Settings.Network.ServerPort).ToString();
            _txtServerIp.Enabled = !_isConnected;
            _txtServerPort.Enabled = !_isConnected;
            nudConcurrentDownloads.Value = ClientConfig.Settings.Download.MaxConcurrentDownloads;

            int currentSpeed = ClientConfig.Settings.Network.RequestedRateMBps;
            int speedIndex = cmbSpeedLimit.Items.IndexOf(currentSpeed.ToString());
            cmbSpeedLimit.SelectedIndex = speedIndex >= 0 ? speedIndex : 1;
        }

        private async Task<bool> SendSpeedLimitAsync(long speedMBs)
        {
            try
            {
                ProtocolPacket response = await _networkService.RequestAsync(new ProtocolPacket
                {
                    Command = PacketCommand.SET_RATE_LIMIT,
                    RequestedRateBytesPerSecond = speedMBs * 1024 * 1024
                });
                bool success = response.Command == PacketCommand.SETTING_RESP && response.Success;
                if (success)
                    SetNotification($"Đã đặt tốc độ tải: {speedMBs} MB/s", isError: false);
                else
                    SetNotification($"Lỗi đặt tốc độ: {response.Message}", isError: true);
                return success;
            }
            catch (Exception ex)
            {
                SetNotification($"Lỗi đặt tốc độ: {ex.Message}", isError: true);
                return false;
            }
        }

        private void btnChangePassword_Click(object? sender, EventArgs e)
        {
            if (!_isConnected)
            {
                SetNotification("Chưa kết nối máy chủ.", isError: true);
                return;
            }

            using var form = new ChangePasswordForm(_networkService, _username);
            form.ShowDialog(this);
        }

        private void RefreshManageFileList()
        {
            dgvManageFile.Rows.Clear();

            var allEntries = new List<(string DisplayName, string FormattedSize, string SavedPath, string DownloadedAt, string Type)>();

            foreach (var f in _uploadFiles)
            {
                string dateStr = "";
                var h = _downloadHistory.FirstOrDefault(x =>
                    string.Equals(x.SavedPath, f.SavedPath, StringComparison.OrdinalIgnoreCase));
                if (h != null)
                    dateStr = h.FormattedDate;

                allEntries.Add((f.DisplayName, f.FormattedSize, f.SavedPath, dateStr, "Upload"));
            }

            foreach (var h in _downloadHistory.Where(h => h.Type == "Upload"))
            {
                bool exists = _uploadFiles.Any(f =>
                    string.Equals(f.SavedPath, h.SavedPath, StringComparison.OrdinalIgnoreCase));
                if (!exists)
                {
                    allEntries.Add((h.DisplayName, h.FormattedSize, h.SavedPath, h.FormattedDate, h.Type));
                }
            }

            for (int i = 0; i < allEntries.Count; i++)
            {
                var e = allEntries[i];
                string typeLabel = e.Type == "Upload" ? "Tải lên" : "Tải xuống";
                dgvManageFile.Rows.Add(
                    i + 1,
                    e.DisplayName,
                    e.FormattedSize,
                    e.SavedPath,
                    e.DownloadedAt,
                    typeLabel
                );
            }
        }

        private void btnNav_Click(object? sender, EventArgs e)
        {
            if (sender is not Button btn) return;

            if (btn == btnNavManageFile)
                SwitchPage(pnlManageFilePage, btnNavManageFile);
            else if (btn == btnNavDownload)
                SwitchPage(pnlDownloadPage, btnNavDownload);
            else if (btn == btnNavSetting)
            {
                LoadSettingsPage();
                SwitchPage(pnlSettingsPage, btnNavSetting);
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  MANAGE FILE TOOLBAR
        // ─────────────────────────────────────────────────────────────

        private async void btnRefresh_Click(object? sender, EventArgs e)
        {
            if (_isConnected)
            {
                SetNotification("Đang làm mới...", isError: false);
                await FetchServerFileListAsync();
            }
            RefreshManageFileList();
        }

        private async void btnRename_Click(object? sender, EventArgs e)
        {
            if (dgvManageFile.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn tệp cần đổi tên.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!_isConnected)
            {
                MessageBox.Show("Chưa kết nối máy chủ.", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var row = dgvManageFile.SelectedRows[0];
            string? oldPath = row.Cells["SavedPath"].Value?.ToString();
            string? oldName = row.Cells["DisplayName"].Value?.ToString();

            if (string.IsNullOrEmpty(oldPath) || string.IsNullOrEmpty(oldName))
                return;

            string? newName = ShowInputDialog("Nhập tên mới:", "Đổi tên tệp", oldName);

            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("Tên tệp không được để trống.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.Equals(newName, oldName, StringComparison.Ordinal))
                return;

            if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageBox.Show("Tên tệp chứa ký tự không hợp lệ.", "Cảnh báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!PacketValidator.HasSameFileExtension(oldName, newName))
            {
                MessageBox.Show("Không được đổi định dạng tệp này.", "Cảnh báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool duplicateName = _serverFiles.Any(f =>
                    string.Equals(f.FileName, newName, StringComparison.OrdinalIgnoreCase)) &&
                !string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase);

            if (duplicateName)
            {
                MessageBox.Show("Tên tệp đã tồn tại, không được trùng nhau.", "Cảnh báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                ProtocolPacket result = await _networkService.RequestAsync(new ProtocolPacket
                {
                    Command = PacketCommand.RENAME_FILE,
                    FileName = oldName,
                    NewFileName = newName
                });
                if (result.Success)
                {
                    var item = _uploadFiles.FirstOrDefault(f => f.SavedPath == oldPath);
                    if (item != null)
                    {
                        item.FileName = newName;
                        item.DisplayName = newName;
                    }

                    var historyItem = _downloadHistory.FirstOrDefault(h =>
                        string.Equals(h.SavedPath, oldPath, StringComparison.OrdinalIgnoreCase));
                    if (historyItem != null)
                    {
                        historyItem.FileName = newName;
                        historyItem.DisplayName = newName;
                        DownloadHistoryStore.Save(_downloadHistory);
                    }

                    await FetchServerFileListAsync();
                    SetNotification($"Đã đổi tên tệp: {oldName} → {newName}", isError: false);
                }
                else
                {
                    string error = result.ErrorCode == "409_FILE_EXISTS"
                        ? "Tên tệp đã tồn tại, không được trùng nhau."
                        : result.Message ?? "Đổi tên tệp thất bại.";
                    MessageBox.Show(error, "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi đổi tên: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnDeleteFile_Click(object? sender, EventArgs e)
        {
            if (dgvManageFile.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn tệp cần xóa.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var rows = dgvManageFile.SelectedRows;
            var names = new List<string>();
            var paths = new List<string>();

            foreach (DataGridViewRow row in rows)
            {
                string? path = row.Cells["SavedPath"].Value?.ToString();
                string? name = row.Cells["DisplayName"].Value?.ToString();
                if (!string.IsNullOrEmpty(path))
                {
                    paths.Add(path);
                    names.Add(name ?? "");
                }
            }

            if (paths.Count == 0) return;

            string msg = paths.Count == 1
                ? $"Bạn có chắc muốn xóa \"{names[0]}\" khỏi máy chủ?"
                : $"Bạn có chắc muốn xóa {paths.Count} tệp khỏi máy chủ?";

            if (MessageBox.Show(msg, "Xác nhận xóa",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            if (!_isConnected)
            {
                MessageBox.Show("Chưa kết nối máy chủ.", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int deleted = 0;
            for (int i = 0; i < paths.Count; i++)
            {
                try
                {
                    string path = paths[i];
                    string fileName = names[i];

                    ProtocolPacket result = await _networkService.RequestAsync(new ProtocolPacket
                    {
                        Command = PacketCommand.DELETE_FILE,
                        FileName = fileName
                    });
                    if (result.Success)
                    {
                        var item = _uploadFiles.FirstOrDefault(f => f.SavedPath == path);
                        if (item != null)
                            _uploadFiles.Remove(item);

                        var historyItem = _downloadHistory.FirstOrDefault(h =>
                            string.Equals(h.SavedPath, path, StringComparison.OrdinalIgnoreCase));
                        if (historyItem != null)
                            _downloadHistory.Remove(historyItem);

                        DownloadHistoryStore.RemoveEntry(path);

                        deleted++;
                    }
                    else
                    {
                        SetNotification($"Lỗi xóa {fileName}: {result.Message}", isError: true);
                    }
                }
                catch (Exception ex)
                {
                    SetNotification($"Lỗi xóa: {ex.Message}", isError: true);
                }
            }

            await FetchServerFileListAsync();
            SetNotification($"Đã xóa {deleted} tệp khỏi máy chủ.", isError: false);
        }

        private async void btnUpload_Click(object? sender, EventArgs e)
        {
            if (!_isConnected)
            {
                MessageBox.Show("Chưa kết nối máy chủ.", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var dialog = new OpenFileDialog();
            dialog.Title = "Chọn tệp để tải lên máy chủ";
            dialog.Multiselect = true;
            dialog.Filter =
                "Định dạng phổ biến|*.pdf;*.doc;*.docx;*.xls;*.xlsx;*.ppt;*.pptx;*.txt;*.csv;*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.mp3;*.wav;*.mp4;*.avi;*.mkv;*.zip;*.rar;*.7z;*.cs;*.cpp;*.java;*.py;*.js;*.json|" +
                "Tài liệu|*.pdf;*.doc;*.docx;*.txt;*.rtf|" +
                "Bảng tính|*.xls;*.xlsx;*.csv|" +
                "Trình chiếu|*.ppt;*.pptx|" +
                "Ảnh|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.svg|" +
                "Video|*.mp4;*.avi;*.mkv;*.mov;*.wmv|" +
                "Âm thanh|*.mp3;*.wav;*.flac;*.aac|" +
                "Tệp nén|*.zip;*.rar;*.7z;*.tar;*.gz|" +
                "Mã nguồn|*.cs;*.cpp;*.h;*.java;*.py;*.js;*.ts;*.json;*.xml;*.html;*.css|" +
                "Tất cả tệp|*.*";

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            int uploaded = 0;
            int failed = 0;
            var uploadManager = new UploadManager();
            int totalFiles = dialog.FileNames.Length;
            _uploadProgressLabel.Visible = true;
            _uploadProgressBar.Visible = true;
            _uploadProgressBar.Value = 0;
            btnUpload.Enabled = false;

            try
            {
                foreach (string filePath in dialog.FileNames)
                {
                    string fileName = Path.GetFileName(filePath);
                    long size = new FileInfo(filePath).Length;
                    _uploadProgressLabel.Text = $"Đang tải {uploaded + failed + 1}/{totalFiles}";
                    _uploadProgressBar.Value = 0;

                    try
                    {
                        SetNotification($"Đang tải lên: {fileName}...", isError: false);

                        var progress = new Progress<int>(value =>
                        {
                            _uploadProgressBar.Value = Math.Clamp(value, 0, 100);
                            _uploadProgressLabel.Text =
                                $"{Path.GetFileName(filePath)} ({_uploadProgressBar.Value}%)";
                        });

                        bool ok = await uploadManager.UploadFileAsync(
                            filePath,
                            _networkService,
                            progress);

                        if (ok)
                        {
                            uploaded++;
                            _uploadFiles.Add(new FileItem
                            {
                                STT = _uploadFiles.Count + 1,
                                FileName = fileName,
                                DisplayName = fileName,
                                FileSizeBytes = size,
                                SavedPath = filePath,
                                Progress = 100,
                                Status = DownloadStatus.Completed
                            });

                            var historyEntry = new DownloadHistoryEntry
                            {
                                FileName = fileName,
                                DisplayName = fileName,
                                FileSizeBytes = size,
                                SavedPath = filePath,
                                DownloadedAt = DateTime.Now,
                                Type = "Upload",
                                Username = _username
                            };
                            _downloadHistory.Add(historyEntry);
                            DownloadHistoryStore.AddEntry(historyEntry);

                            SetNotification($"Đã tải lên: {fileName}", isError: false);
                        }
                        else
                        {
                            failed++;
                            SetNotification($"Lỗi tải lên {fileName}.", isError: true);
                        }
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        SetNotification($"Lỗi tải lên {fileName}: {ex.Message}", isError: true);
                    }
                }

                SetNotification($"Hoàn thành: {uploaded} thành công, {failed} lỗi.", isError: failed > 0);
                await FetchServerFileListAsync();
            }
            finally
            {
                btnUpload.Enabled = true;
                _uploadProgressBar.Value = 0;
                _uploadProgressBar.Visible = false;
                _uploadProgressLabel.Text = "";
                _uploadProgressLabel.Visible = false;
            }
        }

        private void ReconcileUploadedFilesWithServer()
        {
            var serverFileNames = new HashSet<string>(
                _serverFiles.Select(f => f.FileName),
                StringComparer.OrdinalIgnoreCase);

            _uploadFiles.RemoveAll(file =>
                !serverFileNames.Contains(file.FileName));

            bool historyChanged = _downloadHistory.RemoveAll(history =>
                history.Type == "Upload" &&
                !serverFileNames.Contains(history.FileName)) > 0;

            if (historyChanged)
                DownloadHistoryStore.Save(_downloadHistory);
        }

        private string? ShowInputDialog(string title, string prompt, string defaultValue)
        {
            using var form = new Form()
            {
                Text = title,
                Size = new Size(380, 150),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var lbl = new Label() { Left = 15, Top = 15, Text = prompt, AutoSize = true };
            var txt = new TextBox() { Left = 15, Top = 40, Width = 330, Text = defaultValue };
            var btnOk = new Button() { Text = "Đồng ý", DialogResult = DialogResult.OK, Left = 200, Top = 75, Width = 70 };
            var btnCancel = new Button() { Text = "Hủy", DialogResult = DialogResult.Cancel, Left = 280, Top = 75, Width = 70 };

            form.Controls.Add(lbl);
            form.Controls.Add(txt);
            form.Controls.Add(btnOk);
            form.Controls.Add(btnCancel);
            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;

            form.Shown += (s, e) =>
            {
                txt.Focus();
                txt.SelectAll();
            };

            return form.ShowDialog(this) == DialogResult.OK ? txt.Text : null;
        }

        private void txtSearchBox_TextChanged(object? sender, EventArgs e)
        {
            string search = txtSearchBox.Text.Trim().ToLower();
            foreach (DataGridViewRow row in dgvManageFile.Rows)
            {
                string? name = row.Cells["DisplayName"].Value?.ToString()?.ToLower() ?? "";
                row.Visible = string.IsNullOrEmpty(search) || name.Contains(search);
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  CONNECT / DISCONNECT
        // ─────────────────────────────────────────────────────────────

        private void btnRefreshServerList_Click(object? sender, EventArgs e)
        {
            if (!_isConnected)
            {
                SetNotification("Chưa kết nối máy chủ.", isError: true);
                return;
            }
            _ = FetchServerFileListAsync();
        }

        private void btnChooseFolder_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "Chọn thư mục lưu tệp";
            dialog.SelectedPath = _downloadFolder;
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _downloadFolder = dialog.SelectedPath;
                txtSaveFolder.Text = _downloadFolder;
            }
        }
        private void btnShowConnectDialog_Click(object? sender, EventArgs e)
        {
            if (!TryReadServerEndpoint(out string ip, out int port))
                return;

            _ = ConnectToServerAsync(ip, port);
        }

        private async Task ShowConnectionDialogAsync()
        {
            if (_isConnected) return;

            using var dialog = new ConnectionForm();
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                await ConnectToServerAsync(dialog.ServerIp, dialog.ServerPort);
            }
        }

        private bool TryReadServerEndpoint(out string ip, out int port)
        {
            ip = _txtServerIp.Text.Trim();
            port = 0;

            if (!Program.IsValidIPv4Strict(ip) || ip is "0.0.0.0" or "255.255.255.255")
            {
                MessageBox.Show("Địa chỉ IP máy chủ không hợp lệ.", "Lỗi cấu hình",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!int.TryParse(_txtServerPort.Text.Trim(), out port) || port is < 1 or > 65535)
            {
                MessageBox.Show("Cổng phải từ 1 đến 65535.", "Lỗi cấu hình",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private async Task ConnectToServerAsync(string ip, int port)
        {
            if (_isConnecting || _isConnected)
                return;

            _isConnecting = true;
            try
            {
                btnShowConnectDialog.Enabled = false;
                SetConnectionState(false, "● Đang kết nối...");
                lblStatus.ForeColor = Color.Orange;

                await _networkService.ConnectAsync(ip, port);
                _serverIp = ip;
                _serverPort = port;

                using var loginForm = new LoginForm(_networkService);
                if (loginForm.ShowDialog(this) != DialogResult.OK)
                {
                    _networkService.Dispose();
                    SetConnectionState(false);
                    return;
                }

                _username = loginForm.LoggedInUsername;
                _sessionToken = loginForm.LoggedInToken;
                ClientConfig.Settings.Network.ServerIp = ip;
                ClientConfig.Settings.Network.ServerPort = port;
                ClientConfig.Settings.Save();

                SetConnectionState(true);
                _heartbeatTimer.Start();

                LoadDownloadHistory();
                RefreshManageFileList();
                await FetchServerFileListAsync();
            }
            catch (SocketException se)
            {
                SetConnectionState(false, "● Kết nối thất bại");
                string msg = se.SocketErrorCode switch
                {
                    SocketError.ConnectionRefused => "Máy chủ chưa chạy hoặc cổng bị đóng.",
                    SocketError.TimedOut => "Hết thời gian kết nối.",
                    SocketError.HostUnreachable or SocketError.NetworkUnreachable
                        => "Không tới được địa chỉ máy chủ.",
                    _ => se.Message
                };
                MessageBox.Show($"Không kết nối được {ip}:{port}\n{msg}", "Lỗi kết nối",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (OperationCanceledException)
            {
                SetConnectionState(false, "● Kết nối thất bại");
                MessageBox.Show($"Hết thời gian kết nối tới {ip}:{port} (5 giây).", "Hết thời gian",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                SetConnectionState(false, "● Kết nối thất bại");
                MessageBox.Show($"Lỗi kết nối máy chủ ({ip}:{port}): {ex.Message}", "Lỗi kết nối",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isConnecting = false;
                if (_activePage == pnlManageFilePage)
                    RefreshManageFileList();
            }
        }

        private void SetNotification(string text, bool isError)
        {
            if (InvokeRequired) { BeginInvoke(() => SetNotification(text, isError)); return; }

            Color lineColor = isError ? Color.Firebrick : Color.SeaGreen;

            txtNotification.SelectionStart = txtNotification.TextLength;
            txtNotification.SelectionLength = 0;
            txtNotification.SelectionColor = lineColor;
            txtNotification.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
            txtNotification.SelectionColor = txtNotification.ForeColor;
            txtNotification.SelectionStart = txtNotification.TextLength;
            txtNotification.ScrollToCaret();
        }

        private void UpdateDownloadStats()
        {
            int total = _downloadFiles.Count;
            int done = _downloadFiles.Count(f => f.Status == DownloadStatus.Completed);
            int err = _downloadFiles.Count(f => f.Status == DownloadStatus.Error);

            lblDownloadStats.Text = $"Tổng: {total} tệp | Đã tải: {done} | Lỗi: {err}";

            if (_activePage == pnlManageFilePage)
                RefreshManageFileList();
        }

        private async Task HeartbeatAsync()
        {
            if (!_isConnected) return;
            try
            {
                ProtocolPacket resp = await _networkService.RequestAsync(new ProtocolPacket
                {
                    Command = PacketCommand.PING
                });
                if (resp.Command != PacketCommand.PONG)
                    throw new IOException("PONG không hợp lệ.");
            }
            catch
            {
                _heartbeatTimer.Stop();
                _networkService.Dispose();
                SetConnectionState(false, "● Mất kết nối");
            }
        }

        public void SetConnectionState(bool isConnected, string customStatus = "")
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetConnectionState(isConnected, customStatus)));
                return;
            }

            _isConnected = isConnected;

            _txtServerIp.Enabled = !isConnected;
            _txtServerPort.Enabled = !isConnected;

            if (_btnDisconnect != null)
            {
                _btnDisconnect.Enabled = isConnected;
                _btnDisconnect.Visible = isConnected;
            }

            string baseTitle = "UDM11 - Trình tải nhiều tệp";

            if (isConnected)
            {
                lblStatus.Text = string.IsNullOrEmpty(customStatus) ? "● Đã kết nối" : customStatus;
                lblStatus.ForeColor = Color.ForestGreen;
                btnShowConnectDialog.Visible = false;
                this.Text = baseTitle;
            }
            else
            {
                lblStatus.Text = string.IsNullOrEmpty(customStatus) ? "● Chưa kết nối" : customStatus;
                lblStatus.ForeColor = Color.Red;
                btnShowConnectDialog.Visible = true;
                btnShowConnectDialog.Enabled = true;
                this.Text = baseTitle;

                _heartbeatTimer.Stop();

                _serverFiles.Clear();
                _downloadFiles.Clear();
                _uploadFiles.Clear();
                UpdateDownloadStats();
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  FETCH FILE LIST
        // ─────────────────────────────────────────────────────────────

        private async Task FetchServerFileListAsync()
        {
            try
            {
                ProtocolPacket response = await _networkService.RequestAsync(new ProtocolPacket
                {
                    Command = PacketCommand.GET_LIST
                });

                if (response.Command == PacketCommand.ERROR_RESP)
                {
                    Console.WriteLine($"[CLIENT] ERROR_RESP: {response.ErrorCode}");
                    return;
                }

                string rawData = PacketHelper.DecodeTextData(response.DataBase64);

                var refreshedServerFiles = new List<FileItem>();

                string[] lines = rawData.Split(
                    new[] { "\r\n", "\n" },
                    StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    string[] parts = line.Split('|');

                    if (parts.Length < 3)
                        continue;

                    string fileName = parts[0].Trim();

                    long fileSize = 0;
                    long.TryParse(parts[1], out fileSize);

                    string fileHash = parts[2].Trim();

                    refreshedServerFiles.Add(new FileItem
                    {
                        STT = refreshedServerFiles.Count + 1,
                        FileName = fileName,
                        FileSizeBytes = fileSize,
                        FileHash = fileHash
                    });
                }

                _serverFiles.RaiseListChangedEvents = false;
                _serverFiles.Clear();
                foreach (FileItem file in refreshedServerFiles)
                    _serverFiles.Add(file);
                _serverFiles.RaiseListChangedEvents = true;
                _serverFiles.ResetBindings();

                ReconcileUploadedFilesWithServer();
                RefreshManageFileList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLIENT] Fetch lỗi: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  MARQUEE SELECTION (dgvServer)
        // ─────────────────────────────────────────────────────────────

        private void dgvServer_Selection_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            var hit = dgvServer.HitTest(e.X, e.Y);

            if (hit.Type == DataGridViewHitTestType.None ||
                hit.Type == DataGridViewHitTestType.ColumnHeader ||
                hit.Type == DataGridViewHitTestType.RowHeader ||
                hit.Type == DataGridViewHitTestType.TopLeftHeader)
            {
                _isSelecting = true;
                _mouseDownOnEmpty = true;
                _mouseDownForDrag = false;
                _savedSelection.Clear();
                _selectStartPoint = e.Location;
                _selectionRect = Rectangle.Empty;
                dgvServer.ClearSelection();
                dgvServer.Invalidate();
                return;
            }

            _isSelecting = false;
            _mouseDownOnEmpty = false;
            _mouseDownForDrag = true;
            _dragStartPoint = e.Location;

            _savedSelection = dgvServer.SelectedRows
                .Cast<DataGridViewRow>()
                .Where(r => r.Visible)
                .Select(r => r.DataBoundItem)
                .OfType<FileItem>()
                .ToList();

            if (hit.RowIndex >= 0 && dgvServer.Rows[hit.RowIndex].DataBoundItem is FileItem clickedItem)
            {
                if (!_savedSelection.Any(f => ReferenceEquals(f, clickedItem)))
                {
                    _savedSelection.Clear();
                    _savedSelection.Add(clickedItem);
                }
            }
        }

        private void dgvServer_Selection_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_isSelecting && _mouseDownOnEmpty)
            {
                _selectionRect = GetRectangle(_selectStartPoint, e.Location);
                dgvServer.Invalidate();

                dgvServer.ClearSelection();
                foreach (DataGridViewRow row in dgvServer.Rows)
                {
                    if (row.Visible && RowIntersectsRectangle(row, _selectionRect))
                    {
                        row.Selected = true;
                    }
                }
                return;
            }

            if (_mouseDownForDrag && e.Button == MouseButtons.Left)
            {
                int dx = Math.Abs(e.X - _dragStartPoint.X);
                int dy = Math.Abs(e.Y - _dragStartPoint.Y);

                if (dx > SystemInformation.DragSize.Width / 2 ||
                    dy > SystemInformation.DragSize.Height / 2)
                {
                    _mouseDownForDrag = false;

                    var selectedItems = _savedSelection
                        .Where(f => f != null)
                        .ToList();

                    if (selectedItems.Count > 0)
                    {
                        dgvServer.DoDragDrop(selectedItems, DragDropEffects.Copy);
                    }
                }
            }
        }

        private void dgvServer_Selection_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            if (_isSelecting)
            {
                _isSelecting = false;
                _selectionRect = Rectangle.Empty;
                dgvServer.Invalidate();
            }

            _mouseDownOnEmpty = false;
            _mouseDownForDrag = false;
            _savedSelection.Clear();
        }

        private void dgvServer_Selection_Paint(object? sender, PaintEventArgs e)
        {
            if (_isSelecting && _selectionRect.Width > 0 && _selectionRect.Height > 0)
            {
                using (Brush fillBrush = new SolidBrush(Color.FromArgb(50, 51, 153, 255)))
                {
                    e.Graphics.FillRectangle(fillBrush, _selectionRect);
                }

                using (Pen borderPen = new Pen(Color.FromArgb(200, 51, 153, 255), 1))
                {
                    e.Graphics.DrawRectangle(borderPen, _selectionRect);
                }
            }
        }

        private static Rectangle GetRectangle(Point p1, Point p2)
        {
            int x = Math.Min(p1.X, p2.X);
            int y = Math.Min(p1.Y, p2.Y);
            int w = Math.Abs(p1.X - p2.X);
            int h = Math.Abs(p1.Y - p2.Y);
            return new Rectangle(x, y, w, h);
        }

        private bool RowIntersectsRectangle(DataGridViewRow row, Rectangle rect)
        {
            Rectangle rowRect = dgvServer.GetRowDisplayRectangle(row.Index, false);
            return rowRect.IntersectsWith(rect);
        }

        // ─────────────────────────────────────────────────────────────
        //  DRAG & DROP (dgvDownload)
        // ─────────────────────────────────────────────────────────────

        private void dgvDownload_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(typeof(List<FileItem>)))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void dgvDownload_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data == null || !e.Data.GetDataPresent(typeof(List<FileItem>)))
                return;

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

            UpdateDownloadStats();
        }

        private string GetUniqueDownloadName(string fileName)
        {
            var used = new HashSet<string>(
                _downloadFiles.Select(f => f.DisplayName),
                StringComparer.OrdinalIgnoreCase);

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

        // ─────────────────────────────────────────────────────────────
        //  DOWNLOAD QUEUE
        // ─────────────────────────────────────────────────────────────

        private void LoadDownloadHistory()
        {
            var history = DownloadHistoryStore.LoadByUsername(_username);
            _downloadHistory.Clear();
            _downloadHistory.AddRange(history);
        }

        private async Task StartDownloadAsync(FileItem item)
        {
            if (item.Status == DownloadStatus.Completed)
                return;

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
                conflictMode,
                _downloadCts.Token);

            switch (result.Status)
            {
                case DownloadStatusResult.Completed:
                    item.Progress = 100;
                    item.SpeedInfo = "";
                    item.Status = DownloadStatus.Completed;
                    item.DisplayName = Path.GetFileName(result.SavedPath) ?? item.FileName;
                    SetNotification($"Đã tải xong: {result.SavedPath}", isError: false);
                    break;

                case DownloadStatusResult.Skipped:
                    item.Progress = 100;
                    item.SpeedInfo = "";
                    item.Status = DownloadStatus.Completed;
                    SetNotification($"Bỏ qua (đã tồn tại): {item.FileName}", isError: false);
                    break;

                default:
                    item.Progress = 0;
                    item.SpeedInfo = "";
                    item.Status = DownloadStatus.Error;
                    SetNotification($"Lỗi tải {item.FileName}: {result.Message}", isError: true);
                    break;
            }

            UpdateDownloadStats();
        }



        // ─────────────────────────────────────────────────────────────
        //  CELL PAINTING (progress bar + status border)
        // ─────────────────────────────────────────────────────────────

        private void dgvDownload_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string? columnName = dgvDownload.Columns[e.ColumnIndex]?.Name;
            if (string.IsNullOrEmpty(columnName)) return;

            Font fontToUse = e.CellStyle?.Font ?? dgvDownload.Font;

            if (columnName == "ProgressText")
            {
                if (dgvDownload.Rows[e.RowIndex]?.DataBoundItem is FileItem item)
                {
                    DataGridViewPaintParts paintParts = e.PaintParts
                        & ~DataGridViewPaintParts.Focus
                        & ~DataGridViewPaintParts.ContentForeground;
                    e.Paint(e.ClipBounds, paintParts);

                    int percentage = Math.Clamp(item.Progress, 0, 100);

                    if (percentage > 0)
                    {
                        int fillWidth = (e.CellBounds.Width - 4) * percentage / 100;
                        Color progressColor = percentage == 100 ? Color.ForestGreen : Color.DodgerBlue;

                        Rectangle progressRect = new Rectangle(
                            e.CellBounds.X + 2,
                            e.CellBounds.Y + 2,
                            fillWidth,
                            e.CellBounds.Height - 5
                        );

                        using (Brush brush = new SolidBrush(progressColor))
                        {
                            e.Graphics?.FillRectangle(brush, progressRect);
                        }
                    }

                    Rectangle borderRect = new Rectangle(
                        e.CellBounds.X + 2,
                        e.CellBounds.Y + 2,
                        e.CellBounds.Width - 5,
                        e.CellBounds.Height - 5
                    );
                    using (Pen pen = new Pen(Color.LightGray))
                    {
                        e.Graphics?.DrawRectangle(pen, borderRect);
                    }

                    if (!string.IsNullOrEmpty(item.ProgressText) && e.Graphics != null)
                    {
                        Color textColor = percentage > 50 ? Color.White : Color.Black;

                        TextRenderer.DrawText(
                            e.Graphics,
                            item.ProgressText,
                            fontToUse,
                            e.CellBounds,
                            textColor,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding
                        );
                    }

                    e.Handled = true;
                }
            }
            else if (columnName == "StatusText")
            {
                if (dgvDownload.Rows[e.RowIndex]?.DataBoundItem is FileItem item)
                {
                    DataGridViewPaintParts paintParts = DataGridViewPaintParts.Background | DataGridViewPaintParts.Border;
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

                        using (Brush bgBrush = new SolidBrush(bg))
                        {
                            e.Graphics.FillRectangle(bgBrush, e.CellBounds);
                        }

                        TextRenderer.DrawText(
                            e.Graphics,
                            item.StatusText,
                            fontToUse,
                            e.CellBounds,
                            fg,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding
                        );
                    }

                    e.Handled = true;
                }
            }
            else if (columnName == "SpeedInfo")
            {
                if (dgvDownload.Rows[e.RowIndex]?.DataBoundItem is FileItem item)
                {
                    DataGridViewPaintParts paintParts = DataGridViewPaintParts.Background | DataGridViewPaintParts.Border;
                    e.Paint(e.ClipBounds, paintParts);

                    if (e.Graphics != null)
                    {
                        string text = item.Status == DownloadStatus.Downloading && !string.IsNullOrEmpty(item.SpeedInfo)
                            ? item.SpeedInfo
                            : "—";

                        Color color = item.Status == DownloadStatus.Downloading
                            ? Color.DarkOrange
                            : Color.Gray;

                        TextRenderer.DrawText(
                            e.Graphics,
                            text,
                            fontToUse,
                            e.CellBounds,
                            color,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding
                        );
                    }

                    e.Handled = true;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────────────

        private void EnableDoubleBuffering(Control control)
        {
            typeof(Control)
                .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(control, true, null);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _heartbeatTimer.Stop();
            CancelActiveDownloads();

            DownloadHistoryStore.Save(_downloadHistory);

            _networkService?.Dispose();
            base.OnFormClosing(e);
        }

        private void pnlDownloadPage_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
