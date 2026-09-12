using Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Client.Logic;

namespace Client
{
    public partial class MainForm : Form
    {
        private bool _isConnected = false;
        private readonly NetworkService _networkService = new NetworkService();

        private readonly BindingList<FileItem> _serverFiles = new BindingList<FileItem>();
        private readonly BindingList<FileItem> _downloadFiles = new BindingList<FileItem>();

        private readonly DownloadManager _downloadManager = new DownloadManager(3);
        private string _serverIp = "";
        private int _serverPort = 0;

        private string _downloadFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        private bool _isSelecting = false;
        private Point _selectStartPoint;
        private Rectangle _selectionRect = Rectangle.Empty;

        private Point _dragStartPoint = Point.Empty;
        private bool _mouseDownOnEmpty = false;
        private bool _mouseDownForDrag = false;
        private int _mouseDownRowIndex = -1;
        private List<FileItem> _savedSelection = new List<FileItem>();

        private DateTime _lastFetchTime = DateTime.MinValue;
        private readonly TimeSpan _minFetchInterval = TimeSpan.FromSeconds(1);

        public MainForm()
        {
            InitializeComponent();

            SetupGridViews();
            SetupDragAndDrop();
            SetConnectionState(false);
            txtSaveFolder.Text = _downloadFolder;
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
            dgvServer.AllowDrop = false;
            dgvDownload.AllowDrop = true;
            dgvDownload.DragEnter += dgvDownload_DragEnter;
            dgvDownload.DragDrop += dgvDownload_DragDrop;
        }

        // ─────────────────────────────────────────────────────────────
        //  CONNECT / DISCONNECT
        // ─────────────────────────────────────────────────────────────

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
        private async void btnConnect_Click(object sender, EventArgs e)
        {
            if (_isConnected)
            {
                _networkService.Dispose();
                SetConnectionState(false);
                txtNotification.Clear();
                lblLastSaved.Text = "Đã lưu: (chưa có file nào)";
                return;
            }
            string ip = txtServerIp.Text.Trim();
            string portText = txtServerPort.Text.Trim();

            if (!Program.IsValidIPv4Strict(ip))
            {
                MessageBox.Show("Địa chỉ IP Server không hợp lệ!", "Lỗi IP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(portText, out int port) || port <= 0 || port > 65535)
            {
                MessageBox.Show("Port phải nằm trong khoảng từ 1 đến 65535!", "Lỗi Port", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (ip == "0.0.0.0" || ip == "255.255.255.255")
            {
                MessageBox.Show("Địa chỉ IP không hợp lệ!", "Lỗi IP",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                btnConnect.Enabled = false;
                SetConnectionState(false, "● Đang kết nối...");
                lblStatus.ForeColor = Color.Orange;

                await _networkService.ConnectAsync(ip, port);
                _serverIp = ip;
                _serverPort = port;

                SetConnectionState(true);

                _lastFetchTime = DateTime.Now;

                await FetchServerFileListAsync();
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
                btnConnect.Enabled = true;
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
                btnConnect.Text = "Ngắt kết nối";
                txtServerIp.Enabled = false;
                txtServerPort.Enabled = false;
                this.Text = $"{baseTitle} - {GetClientEndpointInfo()}";
            }
            else
            {
                lblStatus.Text = string.IsNullOrEmpty(customStatus) ? "● Chưa kết nối" : customStatus;
                lblStatus.ForeColor = Color.Red;
                btnConnect.Text = "Kết nối";
                txtServerIp.Enabled = true;
                txtServerPort.Enabled = true;

                this.Text = baseTitle;

                _serverFiles.Clear();
                _downloadFiles.Clear();
                UpdateDownloadStats();
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  FETCH FILE LIST
        // ─────────────────────────────────────────────────────────────

        private async Task FetchServerFileListAsync()
        {
            _lastFetchTime = DateTime.Now;

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
        }

        // ─────────────────────────────────────────────────────────────
        //  MARQUEE SELECTION (dgvServer)
        // ─────────────────────────────────────────────────────────────

        private void dgvServer_Selection_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            var hit = dgvServer.HitTest(e.X, e.Y);
            _mouseDownRowIndex = hit.RowIndex;

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
            _mouseDownRowIndex = -1;
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
                   File.Exists(Path.Combine(_downloadFolder, candidate)))
            {
                candidate = $"{name}({n}){ext}";
                n++;
            }

            return candidate;
        }

        // ─────────────────────────────────────────────────────────────
        //  DOWNLOAD QUEUE
        // ─────────────────────────────────────────────────────────────

        private async Task StartDownloadAsync(FileItem item)
        {
            item.Status = DownloadStatus.Pending;
            item.Progress = 0;
            item.SpeedInfo = "Chờ slot...";

            var progress = new Progress<DownloadProgressModel>(p =>
            {
                if (p.SpeedInfo == "Skipped") return;

                item.Progress = p.Percentage;
                item.SpeedInfo = p.SpeedInfo;

                if (item.Status == DownloadStatus.Pending)
                    item.Status = DownloadStatus.Downloading;
            });

            DownloadResult result = await _downloadManager.StartDownloadAsync(
                item.FileName,
                item.FileSizeBytes,
                _serverIp,
                _serverPort,
                _downloadFolder,
                progress,
                FileConflictMode.AutoRename);

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
                    DataGridViewPaintParts paintParts = e.PaintParts & ~DataGridViewPaintParts.Focus;
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
            _networkService?.Dispose();
            base.OnFormClosing(e);
        }
    }
}