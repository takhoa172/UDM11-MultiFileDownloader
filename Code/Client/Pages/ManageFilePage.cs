using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Shared;

namespace Client.Pages
{
    public class ManageFilePage : UserControl
    {
        private readonly NetworkService _networkService;
        private readonly BindingList<FileItem> _serverFiles;
        private readonly List<FileItem> _uploadFiles;
        private readonly List<DownloadHistoryEntry> _downloadHistory;

        private bool _isConnected;

        private Label lblTitle;
        private Panel pnlToolbar;
        private TextBox txtSearchBox;
        private Button btnRefresh;
        private Button btnRename;
        private Button btnDelete;
        private Button btnUpload;
        private DataGridView dgvManageFile;

        public ManageFilePage(
            NetworkService networkService,
            BindingList<FileItem> serverFiles,
            List<FileItem> uploadFiles,
            List<DownloadHistoryEntry> downloadHistory)
        {
            _networkService = networkService;
            _serverFiles = serverFiles;
            _uploadFiles = uploadFiles;
            _downloadHistory = downloadHistory;

            InitializeComponent();
            SetupDataGridView();
        }

        public void SetConnected(bool isConnected)
        {
            _isConnected = isConnected;

            if (InvokeRequired)
            {
                BeginInvoke(() => UpdateButtonStates());
                return;
            }

            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            btnRefresh.Enabled = _isConnected;
            btnRename.Enabled = _isConnected;
            btnDelete.Enabled = _isConnected;
            btnUpload.Enabled = _isConnected;
        }

        // ── UI Setup ──

        private void InitializeComponent()
        {
            this.SuspendLayout();

            lblTitle = new Label();
            pnlToolbar = new Panel();
            txtSearchBox = new TextBox();
            btnRefresh = new Button();
            btnRename = new Button();
            btnDelete = new Button();
            btnUpload = new Button();
            dgvManageFile = new DataGridView();

            pnlToolbar.SuspendLayout();
            ((ISupportInitialize)dgvManageFile).BeginInit();
            this.SuspendLayout();

            // lblTitle
            lblTitle.Dock = DockStyle.Top;
            lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblTitle.ForeColor = Color.SteelBlue;
            lblTitle.Location = new Point(0, 0);
            lblTitle.Size = new Size(1000, 40);
            lblTitle.Text = "Manage File - Da tai xong";
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            lblTitle.Padding = new Padding(10, 0, 0, 0);

            // pnlToolbar
            pnlToolbar.Controls.Add(txtSearchBox);
            pnlToolbar.Controls.Add(btnRefresh);
            pnlToolbar.Controls.Add(btnRename);
            pnlToolbar.Controls.Add(btnDelete);
            pnlToolbar.Controls.Add(btnUpload);
            pnlToolbar.Dock = DockStyle.Top;
            pnlToolbar.Location = new Point(0, 40);
            pnlToolbar.Size = new Size(1000, 45);
            pnlToolbar.Padding = new Padding(10, 8, 10, 8);

            // txtSearchBox
            txtSearchBox.Location = new Point(10, 10);
            txtSearchBox.Size = new Size(280, 25);
            txtSearchBox.PlaceholderText = "Tim kiem file...";
            txtSearchBox.TextChanged += txtSearchBox_TextChanged;

            // btnRefresh
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.Location = new Point(300, 8);
            btnRefresh.Size = new Size(80, 29);
            btnRefresh.Text = "Lam moi";
            btnRefresh.Click += btnRefresh_Click;

            // btnRename
            btnRename.FlatStyle = FlatStyle.Flat;
            btnRename.Location = new Point(390, 8);
            btnRename.Size = new Size(80, 29);
            btnRename.Text = "Doi ten";
            btnRename.Click += btnRename_Click;

            // btnDelete
            btnDelete.FlatStyle = FlatStyle.Flat;
            btnDelete.Location = new Point(480, 8);
            btnDelete.Size = new Size(80, 29);
            btnDelete.Text = "Xoa";
            btnDelete.Click += btnDelete_Click;

            // btnUpload
            btnUpload.FlatStyle = FlatStyle.Flat;
            btnUpload.Location = new Point(570, 8);
            btnUpload.Size = new Size(100, 29);
            btnUpload.Text = "Tai len";
            btnUpload.Click += btnUpload_Click;

            // dgvManageFile
            dgvManageFile.AllowUserToAddRows = false;
            dgvManageFile.AllowUserToDeleteRows = false;
            dgvManageFile.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvManageFile.BackgroundColor = Color.White;
            dgvManageFile.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvManageFile.Dock = DockStyle.Fill;
            dgvManageFile.Location = new Point(0, 85);
            dgvManageFile.MultiSelect = false;
            dgvManageFile.ReadOnly = true;
            dgvManageFile.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvManageFile.Size = new Size(1000, 515);

            this.Controls.Add(dgvManageFile);
            this.Controls.Add(pnlToolbar);
            this.Controls.Add(lblTitle);

            ((ISupportInitialize)dgvManageFile).EndInit();
            pnlToolbar.ResumeLayout(false);
            pnlToolbar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void SetupDataGridView()
        {
            dgvManageFile.Columns.Clear();
            dgvManageFile.Columns.Add(new DataGridViewTextBoxColumn { Name = "STT", HeaderText = "STT", Width = 40 });
            dgvManageFile.Columns.Add(new DataGridViewTextBoxColumn { Name = "DisplayName", HeaderText = "Ten File", FillWeight = 40 });
            dgvManageFile.Columns.Add(new DataGridViewTextBoxColumn { Name = "FormattedSize", HeaderText = "Kich Thuoc", FillWeight = 15 });
            dgvManageFile.Columns.Add(new DataGridViewTextBoxColumn { Name = "SavedPath", HeaderText = "Duong Dan", FillWeight = 30 });
            dgvManageFile.Columns.Add(new DataGridViewTextBoxColumn { Name = "DownloadedAt", HeaderText = "Ngay Tai", FillWeight = 20 });
            dgvManageFile.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Loai", FillWeight = 10 });

            dgvManageFile.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 220, 255);
            dgvManageFile.DefaultCellStyle.SelectionForeColor = Color.Black;

            typeof(DataGridView)
                .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(dgvManageFile, true, null);
        }

        // ── Refresh ──

        public void RefreshList()
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => RefreshList());
                return;
            }

            string searchText = txtSearchBox.Text?.Trim().ToLower() ?? "";
            bool hasFilter = !string.IsNullOrEmpty(searchText);

            dgvManageFile.Rows.Clear();

            int stt = 1;

            foreach (var item in _uploadFiles)
            {
                string displayName = !string.IsNullOrEmpty(item.DisplayName) ? item.DisplayName : item.FileName;
                if (hasFilter && !displayName.ToLower().Contains(searchText) && !item.FileName.ToLower().Contains(searchText))
                    continue;

                dgvManageFile.Rows.Add(
                    stt++,
                    displayName,
                    item.FormattedSize,
                    item.SavedPath,
                    "",
                    "Upload"
                );
            }

            foreach (var entry in _downloadHistory)
            {
                string displayName = !string.IsNullOrEmpty(entry.DisplayName) ? entry.DisplayName : entry.FileName;
                if (hasFilter && !displayName.ToLower().Contains(searchText) && !entry.FileName.ToLower().Contains(searchText))
                    continue;

                dgvManageFile.Rows.Add(
                    stt++,
                    displayName,
                    entry.FormattedSize,
                    entry.SavedPath,
                    entry.FormattedDate,
                    entry.Type
                );
            }
        }

        // ── Search Filter ──

        private void txtSearchBox_TextChanged(object? sender, EventArgs e)
        {
            RefreshList();
        }

        // ── Refresh Button ──

        private async void btnRefresh_Click(object? sender, EventArgs e)
        {
            if (!_isConnected)
            {
                MessageBox.Show("Chua ket noi Server.", "Thong bao", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                btnRefresh.Enabled = false;
                btnRefresh.Text = "Dang tai...";

                await FetchServerFileListAsync();
                RefreshList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Loi tai danh sach: {ex.Message}", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRefresh.Enabled = _isConnected;
                btnRefresh.Text = "Lam moi";
            }
        }

        private async Task FetchServerFileListAsync()
        {
            try
            {
                await _networkService.SendPacketAsync(new ProtocolPacket { Command = PacketCommand.GET_LIST });
                ProtocolPacket response = await _networkService.ReadPacketAsync();

                if (response.Command == PacketCommand.ERROR_RESP)
                    return;

                string rawData = PacketHelper.DecodeTextData(response.DataBase64);
                _serverFiles.Clear();

                if (string.IsNullOrWhiteSpace(rawData))
                    return;

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

        // ── Rename ──

        private async void btnRename_Click(object? sender, EventArgs e)
        {
            if (!_isConnected)
            {
                MessageBox.Show("Chua ket noi Server.", "Thong bao", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dgvManageFile.CurrentRow == null || dgvManageFile.CurrentRow.Index < 0)
            {
                MessageBox.Show("Vui long chon file can doi ten.", "Thong bao", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow row = dgvManageFile.CurrentRow;
            string? currentDisplayName = row.Cells["DisplayName"].Value?.ToString();
            string? savedPath = row.Cells["SavedPath"].Value?.ToString();

            if (string.IsNullOrEmpty(currentDisplayName))
            {
                MessageBox.Show("Khong the lay ten file.", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string? newName = ShowInputDialog("Doi ten file", "Nhap ten moi:", currentDisplayName);
            if (string.IsNullOrEmpty(newName) || newName == currentDisplayName)
                return;

            try
            {
                await _networkService.SendPacketAsync(new ProtocolPacket
                {
                    Command = PacketCommand.RENAME_FILE,
                    FileName = currentDisplayName,
                    NewFileName = newName
                });

                ProtocolPacket response = await _networkService.ReadPacketAsync();

                if (response.Command == PacketCommand.ERROR_RESP)
                {
                    MessageBox.Show(response.Message ?? "Doi ten that bai.", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Update uploadFiles list
                foreach (var item in _uploadFiles)
                {
                    if (item.FileName == currentDisplayName ||
                        (!string.IsNullOrEmpty(item.DisplayName) && item.DisplayName == currentDisplayName))
                    {
                        item.DisplayName = newName;
                        break;
                    }
                }

                // Update downloadHistory
                foreach (var entry in _downloadHistory)
                {
                    if (entry.FileName == currentDisplayName ||
                        (!string.IsNullOrEmpty(entry.DisplayName) && entry.DisplayName == currentDisplayName))
                    {
                        entry.DisplayName = newName;
                        break;
                    }
                }

                DownloadHistoryStore.Save(_downloadHistory);
                RefreshList();

                MessageBox.Show("Doi ten thanh cong!", "Thong bao", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Loi doi ten: {ex.Message}", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Delete ──

        private async void btnDelete_Click(object? sender, EventArgs e)
        {
            if (!_isConnected)
            {
                MessageBox.Show("Chua ket noi Server.", "Thong bao", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dgvManageFile.CurrentRow == null || dgvManageFile.CurrentRow.Index < 0)
            {
                MessageBox.Show("Vui long chon file can xoa.", "Thong bao", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow row = dgvManageFile.CurrentRow;
            string? displayName = row.Cells["DisplayName"].Value?.ToString();
            string? savedPath = row.Cells["SavedPath"].Value?.ToString();

            if (string.IsNullOrEmpty(displayName))
            {
                MessageBox.Show("Khong the lay ten file.", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DialogResult confirm = MessageBox.Show(
                $"Ban co chac muon xoa \"{displayName}\"?",
                "Xac nhan xoa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                btnDelete.Enabled = false;
                btnDelete.Text = "Dang xoa...";

                await _networkService.SendPacketAsync(new ProtocolPacket
                {
                    Command = PacketCommand.DELETE_FILE,
                    FileName = displayName
                });

                ProtocolPacket response = await _networkService.ReadPacketAsync();

                if (response.Command == PacketCommand.ERROR_RESP)
                {
                    MessageBox.Show(response.Message ?? "Xoa that bai.", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Remove from uploadFiles
                var uploadItem = _uploadFiles.FirstOrDefault(f =>
                    f.FileName == displayName ||
                    (!string.IsNullOrEmpty(f.DisplayName) && f.DisplayName == displayName));
                if (uploadItem != null)
                    _uploadFiles.Remove(uploadItem);

                // Remove from downloadHistory
                _downloadHistory.RemoveAll(h =>
                    h.FileName == displayName ||
                    (!string.IsNullOrEmpty(h.DisplayName) && h.DisplayName == displayName));

                if (!string.IsNullOrEmpty(savedPath))
                    DownloadHistoryStore.RemoveEntry(savedPath);

                RefreshList();

                MessageBox.Show("Xoa thanh cong!", "Thong bao", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Loi xoa file: {ex.Message}", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnDelete.Enabled = _isConnected;
                btnDelete.Text = "Xoa";
            }
        }

        // ── Upload ──

        private async void btnUpload_Click(object? sender, EventArgs e)
        {
            if (!_isConnected)
            {
                MessageBox.Show("Chua ket noi Server.", "Thong bao", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Chon file de tai len";
            ofd.Filter = "Tat ca file (*.*)|*.*";
            ofd.Multiselect = false;

            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            string filePath = ofd.FileName;
            FileInfo fileInfo = new FileInfo(filePath);

            if (!fileInfo.Exists)
            {
                MessageBox.Show("File khong ton tai.", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (fileInfo.Length > 3L * 1024 * 1024 * 1024)
            {
                MessageBox.Show("Kich thuoc file vuot qua 3GB.", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                btnUpload.Enabled = false;
                btnUpload.Text = "Dang tai len...";

                // Send UPLOAD_REQ
                await _networkService.SendPacketAsync(new ProtocolPacket
                {
                    Command = PacketCommand.UPLOAD_REQ,
                    FileName = fileInfo.Name,
                    TotalSize = fileInfo.Length
                });

                ProtocolPacket reqResponse = await _networkService.ReadPacketAsync();
                if (reqResponse.Command == PacketCommand.ERROR_RESP)
                {
                    MessageBox.Show(reqResponse.Message ?? "Yeu cau tai len that bai.", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Send file chunks
                using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192, useAsync: true);
                byte[] buffer = new byte[8192];
                int bytesRead;
                long totalBytesRead = 0;

                while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    totalBytesRead += bytesRead;
                    bool isLastChunk = totalBytesRead == fileInfo.Length;

                    string dataBase64 = Convert.ToBase64String(buffer, 0, bytesRead);

                    await _networkService.SendPacketAsync(new ProtocolPacket
                    {
                        Command = PacketCommand.FILE_CHUNK,
                        FileName = fileInfo.Name,
                        DataBase64 = dataBase64,
                        IsLastChunk = isLastChunk
                    });
                }

                // Send UPLOAD_DONE
                await _networkService.SendPacketAsync(new ProtocolPacket
                {
                    Command = PacketCommand.UPLOAD_DONE,
                    FileName = fileInfo.Name
                });

                ProtocolPacket doneResponse = await _networkService.ReadPacketAsync();

                // Add to uploadFiles list
                _uploadFiles.Add(new FileItem
                {
                    STT = _uploadFiles.Count + 1,
                    FileName = fileInfo.Name,
                    DisplayName = fileInfo.Name,
                    FileSizeBytes = fileInfo.Length,
                    SavedPath = filePath
                });

                RefreshList();

                MessageBox.Show($"Tai len thanh cong: {fileInfo.Name}", "Thong bao", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Loi tai len: {ex.Message}", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnUpload.Enabled = _isConnected;
                btnUpload.Text = "Tai len";
            }
        }

        // ── Helper: Input Dialog ──

        private static string? ShowInputDialog(string title, string prompt, string defaultValue = "")
        {
            using Form form = new Form();
            form.Text = title;
            form.Width = 400;
            form.Height = 150;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MaximizeBox = false;
            form.MinimizeBox = false;

            Label lbl = new Label
            {
                Text = prompt,
                Left = 15,
                Top = 15,
                Width = 350
            };

            TextBox txt = new TextBox
            {
                Text = defaultValue,
                Left = 15,
                Top = 45,
                Width = 350
            };

            Button btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Left = 200,
                Top = 80,
                Width = 75
            };

            Button btnCancel = new Button
            {
                Text = "Huy",
                DialogResult = DialogResult.Cancel,
                Left = 290,
                Top = 80,
                Width = 75
            };

            form.Controls.Add(lbl);
            form.Controls.Add(txt);
            form.Controls.Add(btnOk);
            form.Controls.Add(btnCancel);
            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;

            txt.SelectAll();
            txt.Focus();

            return form.ShowDialog() == DialogResult.OK ? txt.Text.Trim() : null;
        }
    }
}
