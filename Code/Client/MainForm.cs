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
        private bool _isConnected = false;
        private bool _mainConnectionBusy = false;
        private readonly NetworkService _networkService;
        private readonly System.Windows.Forms.Timer _heartbeatTimer = new();

        private readonly BindingList<FileItem> _serverFiles = new BindingList<FileItem>();
        private readonly BindingList<FileItem> _downloadFiles = new BindingList<FileItem>();
        private readonly List<FileItem> _uploadFiles = new List<FileItem>();
        private readonly List<DownloadHistoryEntry> _downloadHistory = new List<DownloadHistoryEntry>();
        private string _username = "";

        private readonly DownloadManager _downloadManager =
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

        public MainForm(NetworkService networkService, string serverIp, int serverPort, string username)
        {
            _networkService = networkService;
            _serverIp = serverIp;
            _serverPort = serverPort;
            _username = username;

            InitializeComponent();

            SetupGridViews();
            SetupDragAndDrop();
            SetupManageFilePage();
            SetupSettingsPage();
            SwitchPage(pnlDownloadPage, btnNavDownload);
            txtSaveFolder.Text = _downloadFolder;

            _heartbeatTimer.Interval = 10000;
            _heartbeatTimer.Tick += async (s, e) => await HeartbeatAsync();

            SetConnectionState(true);
            _heartbeatTimer.Start();
            _ = FetchServerFileListAsync();
            RestorePendingDownloads();
            LoadDownloadHistory();
        }

        // ─────────────────────────────────────────────────────────────
        //  SETUP
        // ─────────────────────────────────────────────────────────────

        private void SetupGridViews()
        {
            dgvServer.DataSource = _serverFiles;
            dgvDownload.DataSource = _downloadFiles;

            dgvServer.MultiSelect = true;
            dgvServer.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgvServer.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvDownload.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            dgvServer.AllowUserToResizeColumns = true;
            dgvDownload.AllowUserToResizeColumns = true;

            var colServerFileName = dgvServer.Columns["FileName"];
            if (colServerFileName != null) colServerFileName.HeaderText = "Tên File";

            var colServerDisplayName = dgvServer.Columns["DisplayName"];
            if (colServerDisplayName != null)
                colServerDisplayName.Visible = false;

            var colServerSize = dgvServer.Columns["FormattedSize"];
            if (colServerSize != null) colServerSize.HeaderText = "Kích Thước";

            var colServerHash = dgvServer.Columns["FileHash"];
            if (colServerHash != null) colServerHash.HeaderText = "SHA-256";

            var colDlFileName = dgvDownload.Columns["FileName"];
            if (colDlFileName != null) colDlFileName.Visible = false;

            var colDlDisplayName = dgvDownload.Columns["DisplayName"];
            if (colDlDisplayName != null)
            {
                colDlDisplayName.HeaderText = "Tên File";
                colDlDisplayName.DisplayIndex = 1;
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
                HeaderText = "Tên File",
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

        private void SetupSettingsPage()
        {
            btnChangePassword.Click += btnChangePassword_Click;
            nudConcurrentDownloads.ValueChanged += nudConcurrentDownloads_ValueChanged;
            cmbSpeedLimit.SelectedIndexChanged += cmbSpeedLimit_SelectedIndexChanged;
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
            nudConcurrentDownloads.Value = ClientConfig.Settings.Download.MaxConcurrentDownloads;

            long currentSpeed = ClientConfig.Settings.Download.SpeedLimitMBs;
            int speedIndex = cmbSpeedLimit.Items.IndexOf(currentSpeed.ToString());
            cmbSpeedLimit.SelectedIndex = speedIndex >= 0 ? speedIndex : 1;
        }

        private void nudConcurrentDownloads_ValueChanged(object? sender, EventArgs e)
        {
            int value = (int)nudConcurrentDownloads.Value;
            ClientConfig.Settings.Download.MaxConcurrentDownloads = value;
            ClientConfig.Settings.Save();
        }

        private void cmbSpeedLimit_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbSpeedLimit.SelectedItem == null || !_isConnected) return;

            long speedMBs = long.Parse(cmbSpeedLimit.SelectedItem.ToString()!);
            ClientConfig.Settings.Download.SpeedLimitMBs = speedMBs;
            ClientConfig.Settings.Save();

            _ = SendSpeedLimitAsync(speedMBs);
        }

        private async Task SendSpeedLimitAsync(long speedMBs)
        {
            try
            {
                await _networkService.SendPacketAsync(new ProtocolPacket
                {
                    Command = PacketCommand.SET_SPEED,
                    SpeedLimitMBs = speedMBs
                });

                ProtocolPacket response = await _networkService.ReadPacketAsync();
                if (response.Command == PacketCommand.PONG)
                    SetNotification($"Đã đặt tốc độ tải: {speedMBs} MB/s", isError: false);
                else
                    SetNotification($"Lỗi đặt tốc độ: {response.Message}", isError: true);
            }
            catch (Exception ex)
            {
                SetNotification($"Lỗi đặt tốc độ: {ex.Message}", isError: true);
            }
        }

        private void btnChangePassword_Click(object? sender, EventArgs e)
        {
            if (!_isConnected)
            {
                SetNotification("Chưa kết nối Server.", isError: true);
                return;
            }

            using var form = new ForgotPasswordForm(_networkService);
            form.ShowDialog(this);
        }

        private void RefreshManageFileList()
        {
            dgvManageFile.Rows.Clear();

            if (_serverFiles.Count > 0)
            {
                var serverFileNames = new HashSet<string>(
                    _serverFiles.Select(f => f.FileName),
                    StringComparer.OrdinalIgnoreCase);

                bool removed = _downloadHistory.RemoveAll(h =>
                    h.Type == "Upload" && !serverFileNames.Contains(h.FileName)) > 0;

                if (removed)
                    DownloadHistoryStore.Save(_downloadHistory);
            }

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
                dgvManageFile.Rows.Add(
                    i + 1,
                    e.DisplayName,
                    e.FormattedSize,
                    e.SavedPath,
                    e.DownloadedAt,
                    e.Type
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
                MessageBox.Show("Vui lòng chọn file cần đổi tên.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!_isConnected)
            {
                MessageBox.Show("Chưa kết nối Server.", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var row = dgvManageFile.SelectedRows[0];
            string? oldPath = row.Cells["SavedPath"].Value?.ToString();
            string? oldName = row.Cells["DisplayName"].Value?.ToString();

            if (string.IsNullOrEmpty(oldPath) || string.IsNullOrEmpty(oldName))
                return;

            string? newName = ShowInputDialog("Nhập tên mới:", "Đổi tên file", oldName);
            if (string.IsNullOrWhiteSpace(newName) || newName == oldName)
                return;

            try
            {
                await _networkService.SendPacketAsync(new ProtocolPacket
                {
                    Command = PacketCommand.RENAME_REQ,
                    FileName = oldName,
                    NewFileName = newName
                });

                ProtocolPacket result = await _networkService.ReadPacketAsync();
                if (result.Command == PacketCommand.PONG)
                {
                    var item = _uploadFiles.FirstOrDefault(f => f.SavedPath == oldPath);
                    if (item != null)
                    {
                        item.DisplayName = newName;
                    }

                    var historyItem = _downloadHistory.FirstOrDefault(h =>
                        string.Equals(h.SavedPath, oldPath, StringComparison.OrdinalIgnoreCase));
                    if (historyItem != null)
                    {
                        historyItem.DisplayName = newName;
                        DownloadHistoryStore.Save(_downloadHistory);
                    }

                    RefreshManageFileList();
                    SetNotification($"Đã đổi tên: {oldName} -> {newName}", isError: false);
                }
                else
                {
                    SetNotification($"Lỗi đổi tên: {result.Message}", isError: true);
                }
            }
            catch (Exception ex)
            {
                SetNotification($"Lỗi đổi tên: {ex.Message}", isError: true);
            }
        }

        private async void btnDeleteFile_Click(object? sender, EventArgs e)
        {
            if (dgvManageFile.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn file cần xóa.", "Thông báo",
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
                ? $"Bạn có chắc muốn xóa \"{names[0]}\" khỏi Server?"
                : $"Bạn có chắc muốn xóa {paths.Count} file khỏi Server?";

            if (MessageBox.Show(msg, "Xác nhận xóa",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            if (!_isConnected)
            {
                MessageBox.Show("Chưa kết nối Server.", "Lỗi",
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

                    await _networkService.SendPacketAsync(new ProtocolPacket
                    {
                        Command = PacketCommand.DELETE_REQ,
                        FileName = fileName
                    });

                    ProtocolPacket result = await _networkService.ReadPacketAsync();
                    if (result.Command == PacketCommand.PONG)
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

            RefreshManageFileList();
            SetNotification($"Đã xóa {deleted} file khỏi Server.", isError: false);
        }

        private async void btnUpload_Click(object? sender, EventArgs e)
        {
            if (!_isConnected)
            {
                MessageBox.Show("Chưa kết nối Server.", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_mainConnectionBusy)
            {
                SetNotification("Đang bận, vui lòng thử lại sau.", isError: true);
                return;
            }

            _mainConnectionBusy = true;
            try
            {
                using var dialog = new OpenFileDialog();
                dialog.Title = "Chọn file để tải lên Server";
                dialog.Multiselect = true;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                int uploaded = 0;
                int failed = 0;

                foreach (string filePath in dialog.FileNames)
                {
                    string fileName = Path.GetFileName(filePath);
                    long size = new FileInfo(filePath).Length;

                    try
                    {
                        SetNotification($"Đang tải lên: {fileName}...", isError: false);

                        await _networkService.SendPacketAsync(new ProtocolPacket
                        {
                            Command = PacketCommand.UPLOAD_REQ,
                            FileName = fileName
                        });

                        ProtocolPacket ready = await _networkService.ReadPacketAsync();
                        if (ready.Command != PacketCommand.PONG)
                        {
                            SetNotification($"Lỗi tải lên {fileName}: {ready.Message}", isError: true);
                            failed++;
                            continue;
                        }

                        string actualName = !string.IsNullOrEmpty(ready.FileName)
                            ? ready.FileName
                            : fileName;

                        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        byte[] buffer = new byte[8192];
                        int bytesRead;

                        while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            bool isLast = fs.Position >= fs.Length;
                            await _networkService.SendPacketAsync(new ProtocolPacket
                            {
                                Command = PacketCommand.FILE_CHUNK,
                                FileName = actualName,
                                DataBase64 = PacketHelper.EncodeBinaryData(buffer[..bytesRead]),
                                IsLastChunk = isLast
                            });
                        }

                        ProtocolPacket result = await _networkService.ReadPacketAsync();
                        if (result.Command == PacketCommand.PONG)
                        {
                            uploaded++;
                            _uploadFiles.Add(new FileItem
                            {
                                STT = _uploadFiles.Count + 1,
                                FileName = actualName,
                                DisplayName = actualName,
                                FileSizeBytes = size,
                                SavedPath = filePath,
                                Progress = 100,
                                Status = DownloadStatus.Completed
                            });

                            var historyEntry = new DownloadHistoryEntry
                            {
                                FileName = actualName,
                                DisplayName = actualName,
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
                            SetNotification($"Lỗi tải lên {fileName}: {result.Message}", isError: true);
                        }
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        SetNotification($"Lỗi tải lên {fileName}: {ex.Message}", isError: true);
                    }
                }

                SetNotification($"Hoàn thành: {uploaded} thành công, {failed} lỗi.", isError: failed > 0);
                _ = FetchServerFileListAsync();
            }
            finally
            {
                _mainConnectionBusy = false;
            }
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
            var btnOk = new Button() { Text = "OK", DialogResult = DialogResult.OK, Left = 200, Top = 75, Width = 70 };
            var btnCancel = new Button() { Text = "Hủy", DialogResult = DialogResult.Cancel, Left = 280, Top = 75, Width = 70 };

            form.Controls.Add(lbl);
            form.Controls.Add(txt);
            form.Controls.Add(btnOk);
            form.Controls.Add(btnCancel);
            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;

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
                SetNotification("Chưa kết nối Server.", isError: true);
                return;
            }
            _ = FetchServerFileListAsync();
        }

        private void btnChooseFolder_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "Chọn thư mục lưu file";
            dialog.SelectedPath = _downloadFolder;
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _downloadFolder = dialog.SelectedPath;
                txtSaveFolder.Text = _downloadFolder;
            }
        }
        private void btnShowConnectDialog_Click(object? sender, EventArgs e)
        {
            _ = ShowConnectionDialogAsync();
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

        private async Task ConnectToServerAsync(string ip, int port)
        {
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

                SetConnectionState(true);
                _heartbeatTimer.Start();

                await FetchServerFileListAsync();
                RestorePendingDownloads();
                LoadDownloadHistory();
            }
            catch (SocketException se)
            {
                SetConnectionState(false, "● Kết nối thất bại");
                string msg = se.SocketErrorCode switch
                {
                    SocketError.ConnectionRefused => "Server chưa chạy hoặc Port đóng.",
                    SocketError.TimedOut => "Hết thời gian kết nối (timeout).",
                    SocketError.HostUnreachable or SocketError.NetworkUnreachable
                        => "Không tới được địa chỉ Server.",
                    _ => se.Message
                };
                MessageBox.Show($"Không kết nối được {ip}:{port}\n{msg}", "Lỗi Kết Nối",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (OperationCanceledException)
            {
                SetConnectionState(false, "● Kết nối thất bại");
                MessageBox.Show($"Hết thời gian kết nối tới {ip}:{port} (5 giây).", "Timeout",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                SetConnectionState(false, "● Kết nối thất bại");
                MessageBox.Show($"Lỗi kết nối Server ({ip}:{port}): {ex.Message}", "Lỗi Kết Nối",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _mainConnectionBusy = false;

                if (_activePage == pnlManageFilePage)
                    RefreshManageFileList();
            }
        }

        private void SetNotification(string text, bool isError)
        {
            if (InvokeRequired) { BeginInvoke(() => SetNotification(text, isError)); return; }

            txtNotification.AppendText(text + Environment.NewLine);
            txtNotification.SelectionStart = txtNotification.TextLength;
            txtNotification.ScrollToCaret();
        }

        private void UpdateDownloadStats()
        {
            int total = _downloadFiles.Count;
            int done = _downloadFiles.Count(f => f.Status == DownloadStatus.Completed);
            int err = _downloadFiles.Count(f => f.Status == DownloadStatus.Error);

            lblDownloadStats.Text = $"Tổng: {total} file | Đã tải: {done} | Lỗi: {err}";

            if (_activePage == pnlManageFilePage)
                RefreshManageFileList();
        }

        private async Task HeartbeatAsync()
        {
            if (!_isConnected || _mainConnectionBusy) return;

            _mainConnectionBusy = true;
            try
            {
                await _networkService.SendPacketAsync(new ProtocolPacket
                {
                    Command = PacketCommand.PING
                });

                ProtocolPacket resp = await _networkService.ReadPacketAsync();
                if (resp.Command != PacketCommand.PONG)
                    throw new IOException("PONG không hợp lệ.");
            }
            catch
            {
                _heartbeatTimer.Stop();
                _networkService.Dispose();
                SetConnectionState(false, "● Mất kết nối");
            }
            finally
            {
                _mainConnectionBusy = false;
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
            string baseTitle = "UDM11 - Multi-File Downloader";

            if (isConnected)
            {
                lblStatus.Text = string.IsNullOrEmpty(customStatus) ? "● Đã kết nối" : customStatus;
                lblStatus.ForeColor = Color.ForestGreen;
                btnShowConnectDialog.Visible = false;
                lblConnectedInfo.Visible = true;
                lblConnectedInfo.Text = $"Server: {_serverIp}:{_serverPort}";
                this.Text = $"{baseTitle} - {GetClientEndpointInfo()}";
            }
            else
            {
                lblStatus.Text = string.IsNullOrEmpty(customStatus) ? "● Chưa kết nối" : customStatus;
                lblStatus.ForeColor = Color.Red;
                btnShowConnectDialog.Visible = true;
                btnShowConnectDialog.Enabled = true;
                lblConnectedInfo.Visible = false;
                lblConnectedInfo.Text = "";

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
            _mainConnectionBusy = true;
            try
            {
                await _networkService.SendPacketAsync(new ProtocolPacket
                {
                    Command = PacketCommand.GET_LIST
                });

                ProtocolPacket response = await _networkService.ReadPacketAsync();

                if (response.Command == PacketCommand.ERROR_RESP)
                {
                    Console.WriteLine($"[CLIENT] ERROR_RESP: {response.ErrorCode}");
                    return;
                }

                string rawData = PacketHelper.DecodeTextData(response.DataBase64);

                _serverFiles.Clear();

                if (string.IsNullOrWhiteSpace(rawData))
                    return;

                string[] lines = rawData.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    string[] parts = line.Split('|');

                    if (parts.Length < 3)
                        continue;

                    string fileName = parts[0].Trim();

                    long fileSize = 0;
                    long.TryParse(parts[1], out fileSize);

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
                Console.WriteLine($"[CLIENT] Fetch lỗi: {ex.Message}");
            }
            finally
            {
                _mainConnectionBusy = false;
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

        private void RestorePendingDownloads()
        {
            var savedStates = DownloadStateStore.Load();
            if (savedStates.Count == 0) return;

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

            DownloadStateStore.Clear();
            UpdateDownloadStats();
            SetNotification($"Khôi phục {_downloadFiles.Count} file đang tải...", isError: false);
        }

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
                progress,
                conflictMode);

            switch (result.Status)
            {
                case DownloadStatusResult.Completed:
                    item.Progress = 100;
                    item.SpeedInfo = "";
                    item.Status = DownloadStatus.Completed;
                    item.DisplayName = Path.GetFileName(result.SavedPath) ?? item.FileName;
                    lblLastSaved.Text = "Đã lưu: " + result.SavedPath;
                    SetNotification($"Đã tải xong: {Path.GetFileName(result.SavedPath)}", isError: false);
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

        private string GetClientEndpointInfo()
        {
            try
            {
                if (_networkService.ClientSocket?.LocalEndPoint is System.Net.IPEndPoint endPoint)
                {
                    return $"{endPoint.Address}:{endPoint.Port}";
                }

                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(ip))
                    {
                        return ip.ToString();
                    }
                }
            }
            catch
            {
            }
            return "127.0.0.1";
        }

        private void EnableDoubleBuffering(Control control)
        {
            typeof(Control)
                .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(control, true, null);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _heartbeatTimer.Stop();

            var statesToSave = _downloadFiles
                .Where(f => f.Status == DownloadStatus.Downloading
                         || f.Status == DownloadStatus.Pending)
                .Select(f =>
                {
                    long downloaded = f.DownloadedBytes;
                    try
                    {
                        if (!string.IsNullOrEmpty(f.SavedPath) && File.Exists(f.SavedPath))
                            downloaded = new FileInfo(f.SavedPath).Length;
                    }
                    catch { }

                    return new DownloadState
                    {
                        FileName = f.FileName,
                        DisplayName = f.DisplayName,
                        FileSizeBytes = f.FileSizeBytes,
                        DownloadedBytes = downloaded,
                        SavedPath = f.SavedPath
                    };
                })
                .ToList();

            DownloadStateStore.Save(statesToSave);
            DownloadHistoryStore.Save(_downloadHistory);

            _networkService?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
