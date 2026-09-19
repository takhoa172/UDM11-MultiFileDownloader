namespace Client
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            pnlSidebar = new Panel();
            lblSidebarTitle = new Label();
            btnNavManageFile = new Button();
            btnNavDownload = new Button();
            btnNavSetting = new Button();
            pnlContent = new Panel();
            pnlDownloadPage = new Panel();
            gbConnection = new GroupBox();
            lblStatus = new Label();
            btnRefreshServerList = new Button();
            btnShowConnectDialog = new Button();
            lblConnectedInfo = new Label();
            lblSaveFolder = new Label();
            txtSaveFolder = new TextBox();
            btnChooseFolder = new Button();
            gbServerFiles = new GroupBox();
            pnlServerToolbar = new Panel();
            dgvServer = new DataGridView();
            gbDownloads = new GroupBox();
            dgvDownload = new DataGridView();
            lblLastSaved = new Label();
            lblDownloadStats = new Label();
            txtNotification = new TextBox();
            pnlManageFilePage = new Panel();
            dgvManageFile = new DataGridView();
            pnlManageFileToolbar = new Panel();
            txtSearchBox = new TextBox();
            btnRefresh = new Button();
            btnRename = new Button();
            btnDeleteFile = new Button();
            btnUpload = new Button();
            lblManageFileTitle = new Label();
            pnlSettingsPage = new Panel();
            lblSettingsTitle = new Label();
            lblSettingsUsername = new Label();
            lblSettingsUsernameValue = new Label();
            btnChangePassword = new Button();
            lblConcurrentDownloads = new Label();
            nudConcurrentDownloads = new NumericUpDown();
            lblSpeedLimit = new Label();
            cmbSpeedLimit = new ComboBox();
            lblSpeedUnit = new Label();
            pnlSidebar.SuspendLayout();
            pnlContent.SuspendLayout();
            pnlDownloadPage.SuspendLayout();
            gbConnection.SuspendLayout();
            gbServerFiles.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvServer).BeginInit();
            gbDownloads.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvDownload).BeginInit();
            pnlManageFilePage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvManageFile).BeginInit();
            pnlManageFileToolbar.SuspendLayout();
            pnlSettingsPage.SuspendLayout();
            SuspendLayout();
            // 
            // pnlSidebar
            // 
            pnlSidebar.BackColor = Color.FromArgb(30, 30, 60);
            pnlSidebar.Controls.Add(lblSidebarTitle);
            pnlSidebar.Controls.Add(btnNavManageFile);
            pnlSidebar.Controls.Add(btnNavDownload);
            pnlSidebar.Controls.Add(btnNavSetting);
            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.Location = new Point(0, 0);
            pnlSidebar.Name = "pnlSidebar";
            pnlSidebar.Size = new Size(180, 661);
            pnlSidebar.TabIndex = 0;
            // 
            // lblSidebarTitle
            // 
            lblSidebarTitle.Dock = DockStyle.Top;
            lblSidebarTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblSidebarTitle.ForeColor = Color.White;
            lblSidebarTitle.Location = new Point(0, 135);
            lblSidebarTitle.Name = "lblSidebarTitle";
            lblSidebarTitle.Size = new Size(180, 50);
            lblSidebarTitle.TabIndex = 0;
            lblSidebarTitle.Text = "UDM11";
            lblSidebarTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnNavManageFile
            // 
            btnNavManageFile.BackColor = Color.FromArgb(30, 30, 60);
            btnNavManageFile.Dock = DockStyle.Top;
            btnNavManageFile.FlatAppearance.BorderSize = 0;
            btnNavManageFile.FlatStyle = FlatStyle.Flat;
            btnNavManageFile.Font = new Font("Segoe UI", 10F);
            btnNavManageFile.ForeColor = Color.White;
            btnNavManageFile.Location = new Point(0, 90);
            btnNavManageFile.Name = "btnNavManageFile";
            btnNavManageFile.Padding = new Padding(15, 0, 0, 0);
            btnNavManageFile.Size = new Size(180, 45);
            btnNavManageFile.TabIndex = 1;
            btnNavManageFile.Text = "Manage File";
            btnNavManageFile.TextAlign = ContentAlignment.MiddleLeft;
            btnNavManageFile.UseVisualStyleBackColor = false;
            btnNavManageFile.Click += btnNav_Click;
            // 
            // btnNavDownload
            // 
            btnNavDownload.BackColor = Color.FromArgb(50, 50, 90);
            btnNavDownload.Dock = DockStyle.Top;
            btnNavDownload.FlatAppearance.BorderSize = 0;
            btnNavDownload.FlatStyle = FlatStyle.Flat;
            btnNavDownload.Font = new Font("Segoe UI", 10F);
            btnNavDownload.ForeColor = Color.White;
            btnNavDownload.Location = new Point(0, 45);
            btnNavDownload.Name = "btnNavDownload";
            btnNavDownload.Padding = new Padding(15, 0, 0, 0);
            btnNavDownload.Size = new Size(180, 45);
            btnNavDownload.TabIndex = 2;
            btnNavDownload.Text = "Download";
            btnNavDownload.TextAlign = ContentAlignment.MiddleLeft;
            btnNavDownload.UseVisualStyleBackColor = false;
            btnNavDownload.Click += btnNav_Click;
            // 
            // btnNavSetting
            // 
            btnNavSetting.BackColor = Color.FromArgb(30, 30, 60);
            btnNavSetting.Dock = DockStyle.Top;
            btnNavSetting.FlatAppearance.BorderSize = 0;
            btnNavSetting.FlatStyle = FlatStyle.Flat;
            btnNavSetting.Font = new Font("Segoe UI", 10F);
            btnNavSetting.ForeColor = Color.White;
            btnNavSetting.Location = new Point(0, 0);
            btnNavSetting.Name = "btnNavSetting";
            btnNavSetting.Padding = new Padding(15, 0, 0, 0);
            btnNavSetting.Size = new Size(180, 45);
            btnNavSetting.TabIndex = 3;
            btnNavSetting.Text = "Setting";
            btnNavSetting.TextAlign = ContentAlignment.MiddleLeft;
            btnNavSetting.UseVisualStyleBackColor = false;
            btnNavSetting.Click += btnNav_Click;
            // 
            // pnlContent
            // 
            pnlContent.Controls.Add(pnlDownloadPage);
            pnlContent.Controls.Add(pnlManageFilePage);
            pnlContent.Controls.Add(pnlSettingsPage);
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.Location = new Point(180, 0);
            pnlContent.Name = "pnlContent";
            pnlContent.Size = new Size(1004, 661);
            pnlContent.TabIndex = 1;
            // 
            // pnlDownloadPage
            // 
            pnlDownloadPage.Controls.Add(gbConnection);
            pnlDownloadPage.Controls.Add(gbServerFiles);
            pnlDownloadPage.Controls.Add(gbDownloads);
            pnlDownloadPage.Controls.Add(lblLastSaved);
            pnlDownloadPage.Controls.Add(lblDownloadStats);
            pnlDownloadPage.Controls.Add(txtNotification);
            pnlDownloadPage.Dock = DockStyle.Fill;
            pnlDownloadPage.Location = new Point(0, 0);
            pnlDownloadPage.Name = "pnlDownloadPage";
            pnlDownloadPage.Size = new Size(1004, 661);
            pnlDownloadPage.TabIndex = 0;
            // 
            // gbConnection
            // 
            gbConnection.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            gbConnection.Controls.Add(lblStatus);
            gbConnection.Controls.Add(btnShowConnectDialog);
            gbConnection.Controls.Add(lblConnectedInfo);
            gbConnection.Controls.Add(lblSaveFolder);
            gbConnection.Controls.Add(txtSaveFolder);
            gbConnection.Controls.Add(btnChooseFolder);
            gbConnection.Location = new Point(12, 12);
            gbConnection.Name = "gbConnection";
            gbConnection.Size = new Size(980, 80);
            gbConnection.TabIndex = 0;
            gbConnection.TabStop = false;
            gbConnection.Text = "Kết nối Server";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            lblStatus.ForeColor = Color.Red;
            lblStatus.Location = new Point(25, 30);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(87, 17);
            lblStatus.TabIndex = 0;
            lblStatus.Text = "Chưa kết nối";
            // 
            // pnlServerToolbar
            // 
            pnlServerToolbar.Controls.Add(btnRefreshServerList);
            pnlServerToolbar.Dock = DockStyle.Top;
            pnlServerToolbar.Name = "pnlServerToolbar";
            pnlServerToolbar.Size = new Size(394, 35);
            pnlServerToolbar.TabIndex = 1;
            // 
            // btnRefreshServerList
            // 
            btnRefreshServerList.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRefreshServerList.AutoSize = true;
            btnRefreshServerList.FlatStyle = FlatStyle.Flat;
            btnRefreshServerList.Location = new Point(300, 4);
            btnRefreshServerList.Name = "btnRefreshServerList";
            btnRefreshServerList.Size = new Size(80, 29);
            btnRefreshServerList.TabIndex = 0;
            btnRefreshServerList.Text = "Làm mới";
            btnRefreshServerList.UseVisualStyleBackColor = true;
            btnRefreshServerList.Click += btnRefreshServerList_Click;
            // 
            // btnShowConnectDialog
            // 
            btnShowConnectDialog.Location = new Point(25, 50);
            btnShowConnectDialog.Name = "btnShowConnectDialog";
            btnShowConnectDialog.Size = new Size(130, 25);
            btnShowConnectDialog.TabIndex = 1;
            btnShowConnectDialog.Text = "Kết nối Server";
            btnShowConnectDialog.UseVisualStyleBackColor = true;
            btnShowConnectDialog.Click += btnShowConnectDialog_Click;
            // 
            // lblConnectedInfo
            // 
            lblConnectedInfo.AutoSize = true;
            lblConnectedInfo.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            lblConnectedInfo.ForeColor = Color.ForestGreen;
            lblConnectedInfo.Location = new Point(170, 30);
            lblConnectedInfo.Name = "lblConnectedInfo";
            lblConnectedInfo.Size = new Size(0, 17);
            lblConnectedInfo.TabIndex = 2;
            lblConnectedInfo.Visible = false;
            // 
            // lblSaveFolder
            // 
            lblSaveFolder.AutoSize = true;
            lblSaveFolder.Location = new Point(450, 28);
            lblSaveFolder.Name = "lblSaveFolder";
            lblSaveFolder.Size = new Size(87, 17);
            lblSaveFolder.TabIndex = 3;
            lblSaveFolder.Text = "Thư mục lưu: ";
            // 
            // txtSaveFolder
            // 
            txtSaveFolder.Location = new Point(450, 45);
            txtSaveFolder.Name = "txtSaveFolder";
            txtSaveFolder.ReadOnly = true;
            txtSaveFolder.Size = new Size(430, 25);
            txtSaveFolder.TabIndex = 4;
            txtSaveFolder.TabStop = false;
            // 
            // btnChooseFolder
            // 
            btnChooseFolder.Location = new Point(886, 43);
            btnChooseFolder.Name = "btnChooseFolder";
            btnChooseFolder.Size = new Size(70, 27);
            btnChooseFolder.TabIndex = 5;
            btnChooseFolder.Text = "Chọn...";
            btnChooseFolder.UseVisualStyleBackColor = true;
            btnChooseFolder.Click += btnChooseFolder_Click;
            // 
            // gbServerFiles
            // 
            gbServerFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            gbServerFiles.Controls.Add(dgvServer);
            gbServerFiles.Controls.Add(pnlServerToolbar);
            gbServerFiles.Location = new Point(12, 98);
            gbServerFiles.Name = "gbServerFiles";
            gbServerFiles.Size = new Size(400, 440);
            gbServerFiles.TabIndex = 1;
            gbServerFiles.TabStop = false;
            gbServerFiles.Text = "Danh sách File trên Server";
            // 
            // dgvServer
            // 
            dgvServer.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvServer.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvServer.Dock = DockStyle.Fill;
            dgvServer.Location = new Point(3, 21);
            dgvServer.Name = "dgvServer";
            dgvServer.ReadOnly = true;
            dgvServer.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvServer.Size = new Size(394, 416);
            dgvServer.TabIndex = 0;
            // 
            // gbDownloads
            // 
            gbDownloads.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            gbDownloads.Controls.Add(dgvDownload);
            gbDownloads.Location = new Point(418, 98);
            gbDownloads.Name = "gbDownloads";
            gbDownloads.Size = new Size(574, 440);
            gbDownloads.TabIndex = 2;
            gbDownloads.TabStop = false;
            gbDownloads.Text = "Khu vực Download (Kéo thả file vào đây)";
            // 
            // dgvDownload
            // 
            dgvDownload.AllowDrop = true;
            dgvDownload.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvDownload.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvDownload.Dock = DockStyle.Fill;
            dgvDownload.Location = new Point(3, 21);
            dgvDownload.Name = "dgvDownload";
            dgvDownload.ReadOnly = true;
            dgvDownload.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDownload.Size = new Size(568, 416);
            dgvDownload.TabIndex = 0;
            // 
            // lblLastSaved
            // 
            lblLastSaved.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblLastSaved.AutoSize = true;
            lblLastSaved.ForeColor = Color.ForestGreen;
            lblLastSaved.Location = new Point(12, 548);
            lblLastSaved.Name = "lblLastSaved";
            lblLastSaved.Size = new Size(154, 17);
            lblLastSaved.TabIndex = 3;
            lblLastSaved.Text = "Đã lưu: (chưa có file nào)";
            // 
            // lblDownloadStats
            // 
            lblDownloadStats.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblDownloadStats.AutoSize = true;
            lblDownloadStats.ForeColor = Color.SteelBlue;
            lblDownloadStats.Location = new Point(200, 548);
            lblDownloadStats.Name = "lblDownloadStats";
            lblDownloadStats.Size = new Size(174, 17);
            lblDownloadStats.TabIndex = 4;
            lblDownloadStats.Text = "Tổng: 0 file | Đã tải: 0 | Lỗi: 0";
            // 
            // txtNotification
            // 
            txtNotification.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtNotification.BackColor = Color.White;
            txtNotification.ForeColor = Color.ForestGreen;
            txtNotification.Location = new Point(12, 572);
            txtNotification.Multiline = true;
            txtNotification.Name = "txtNotification";
            txtNotification.ReadOnly = true;
            txtNotification.ScrollBars = ScrollBars.Vertical;
            txtNotification.Size = new Size(980, 70);
            txtNotification.TabIndex = 5;
            txtNotification.TabStop = false;
            // 
            // pnlManageFilePage
            // 
            pnlManageFilePage.Controls.Add(dgvManageFile);
            pnlManageFilePage.Controls.Add(pnlManageFileToolbar);
            pnlManageFilePage.Controls.Add(lblManageFileTitle);
            pnlManageFilePage.Dock = DockStyle.Fill;
            pnlManageFilePage.Location = new Point(0, 0);
            pnlManageFilePage.Name = "pnlManageFilePage";
            pnlManageFilePage.Size = new Size(1004, 661);
            pnlManageFilePage.TabIndex = 1;
            pnlManageFilePage.Visible = false;
            // 
            // dgvManageFile
            // 
            dgvManageFile.AllowUserToAddRows = false;
            dgvManageFile.AllowUserToDeleteRows = false;
            dgvManageFile.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvManageFile.BackgroundColor = Color.White;
            dgvManageFile.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvManageFile.Dock = DockStyle.Fill;
            dgvManageFile.Location = new Point(0, 90);
            dgvManageFile.Name = "dgvManageFile";
            dgvManageFile.ReadOnly = true;
            dgvManageFile.RowHeadersWidth = 51;
            dgvManageFile.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvManageFile.Size = new Size(1004, 571);
            dgvManageFile.TabIndex = 0;
            // 
            // pnlManageFileToolbar
            // 
            pnlManageFileToolbar.BackColor = Color.FromArgb(240, 240, 245);
            pnlManageFileToolbar.Controls.Add(txtSearchBox);
            pnlManageFileToolbar.Controls.Add(btnRefresh);
            pnlManageFileToolbar.Controls.Add(btnRename);
            pnlManageFileToolbar.Controls.Add(btnDeleteFile);
            pnlManageFileToolbar.Controls.Add(btnUpload);
            pnlManageFileToolbar.Dock = DockStyle.Top;
            pnlManageFileToolbar.Location = new Point(0, 40);
            pnlManageFileToolbar.Name = "pnlManageFileToolbar";
            pnlManageFileToolbar.Size = new Size(1004, 50);
            pnlManageFileToolbar.TabIndex = 2;
            // 
            // txtSearchBox
            // 
            txtSearchBox.Location = new Point(10, 12);
            txtSearchBox.Name = "txtSearchBox";
            txtSearchBox.PlaceholderText = "Tìm kiếm file...";
            txtSearchBox.Size = new Size(260, 25);
            txtSearchBox.TabIndex = 0;
            txtSearchBox.TextChanged += txtSearchBox_TextChanged;
            // 
            // btnRefresh
            // 
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.Location = new Point(280, 10);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(90, 30);
            btnRefresh.TabIndex = 1;
            btnRefresh.Text = "Làm mới";
            btnRefresh.UseVisualStyleBackColor = true;
            btnRefresh.Click += btnRefresh_Click;
            // 
            // btnRename
            // 
            btnRename.FlatStyle = FlatStyle.Flat;
            btnRename.Location = new Point(380, 10);
            btnRename.Name = "btnRename";
            btnRename.Size = new Size(90, 30);
            btnRename.TabIndex = 2;
            btnRename.Text = "Đổi tên";
            btnRename.UseVisualStyleBackColor = true;
            btnRename.Click += btnRename_Click;
            // 
            // btnDeleteFile
            // 
            btnDeleteFile.FlatStyle = FlatStyle.Flat;
            btnDeleteFile.ForeColor = Color.Red;
            btnDeleteFile.Location = new Point(480, 10);
            btnDeleteFile.Name = "btnDeleteFile";
            btnDeleteFile.Size = new Size(90, 30);
            btnDeleteFile.TabIndex = 3;
            btnDeleteFile.Text = "Xóa";
            btnDeleteFile.UseVisualStyleBackColor = true;
            btnDeleteFile.Click += btnDeleteFile_Click;
            // 
            // btnUpload
            // 
            btnUpload.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnUpload.FlatStyle = FlatStyle.Flat;
            btnUpload.Location = new Point(894, 10);
            btnUpload.Name = "btnUpload";
            btnUpload.Size = new Size(100, 30);
            btnUpload.TabIndex = 4;
            btnUpload.Text = "Tải lên";
            btnUpload.UseVisualStyleBackColor = true;
            btnUpload.Click += btnUpload_Click;
            // 
            // lblManageFileTitle
            // 
            lblManageFileTitle.BackColor = Color.FromArgb(240, 240, 245);
            lblManageFileTitle.Dock = DockStyle.Top;
            lblManageFileTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblManageFileTitle.Location = new Point(0, 0);
            lblManageFileTitle.Name = "lblManageFileTitle";
            lblManageFileTitle.Size = new Size(1004, 40);
            lblManageFileTitle.TabIndex = 1;
            lblManageFileTitle.Text = "  Manage File - Đã tải xong";
            lblManageFileTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // pnlSettingsPage
            // 
            pnlSettingsPage.Controls.Add(lblSettingsTitle);
            pnlSettingsPage.Controls.Add(lblSettingsUsername);
            pnlSettingsPage.Controls.Add(lblSettingsUsernameValue);
            pnlSettingsPage.Controls.Add(btnChangePassword);
            pnlSettingsPage.Controls.Add(lblConcurrentDownloads);
            pnlSettingsPage.Controls.Add(nudConcurrentDownloads);
            pnlSettingsPage.Controls.Add(lblSpeedLimit);
            pnlSettingsPage.Controls.Add(cmbSpeedLimit);
            pnlSettingsPage.Controls.Add(lblSpeedUnit);
            pnlSettingsPage.Dock = DockStyle.Fill;
            pnlSettingsPage.Location = new Point(0, 0);
            pnlSettingsPage.Name = "pnlSettingsPage";
            pnlSettingsPage.Size = new Size(1004, 661);
            pnlSettingsPage.TabIndex = 2;
            pnlSettingsPage.Visible = false;
            // 
            // lblSettingsTitle
            // 
            lblSettingsTitle.BackColor = Color.FromArgb(240, 240, 245);
            lblSettingsTitle.Dock = DockStyle.Top;
            lblSettingsTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblSettingsTitle.Location = new Point(0, 0);
            lblSettingsTitle.Name = "lblSettingsTitle";
            lblSettingsTitle.Size = new Size(1004, 40);
            lblSettingsTitle.TabIndex = 0;
            lblSettingsTitle.Text = "  Cài đặt";
            lblSettingsTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblSettingsUsername
            // 
            lblSettingsUsername.AutoSize = true;
            lblSettingsUsername.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblSettingsUsername.Location = new Point(30, 65);
            lblSettingsUsername.Name = "lblSettingsUsername";
            lblSettingsUsername.Size = new Size(120, 19);
            lblSettingsUsername.TabIndex = 1;
            lblSettingsUsername.Text = "Tài khoản:";
            // 
            // lblSettingsUsernameValue
            // 
            lblSettingsUsernameValue.AutoSize = true;
            lblSettingsUsernameValue.Font = new Font("Segoe UI", 10F);
            lblSettingsUsernameValue.ForeColor = Color.ForestGreen;
            lblSettingsUsernameValue.Location = new Point(160, 65);
            lblSettingsUsernameValue.Name = "lblSettingsUsernameValue";
            lblSettingsUsernameValue.Size = new Size(0, 19);
            lblSettingsUsernameValue.TabIndex = 2;
            // 
            // btnChangePassword
            // 
            btnChangePassword.FlatStyle = FlatStyle.Flat;
            btnChangePassword.Location = new Point(30, 100);
            btnChangePassword.Name = "btnChangePassword";
            btnChangePassword.Size = new Size(160, 32);
            btnChangePassword.TabIndex = 3;
            btnChangePassword.Text = "Đổi mật khẩu";
            btnChangePassword.UseVisualStyleBackColor = true;
            // 
            // lblConcurrentDownloads
            // 
            lblConcurrentDownloads.AutoSize = true;
            lblConcurrentDownloads.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblConcurrentDownloads.Location = new Point(30, 160);
            lblConcurrentDownloads.Name = "lblConcurrentDownloads";
            lblConcurrentDownloads.Size = new Size(200, 19);
            lblConcurrentDownloads.TabIndex = 4;
            lblConcurrentDownloads.Text = "Số file tải đồng thời:";
            // 
            // nudConcurrentDownloads
            // 
            nudConcurrentDownloads.Location = new Point(250, 158);
            nudConcurrentDownloads.Maximum = new decimal(new int[] { 5, 0, 0, 0 });
            nudConcurrentDownloads.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudConcurrentDownloads.Name = "nudConcurrentDownloads";
            nudConcurrentDownloads.Size = new Size(80, 25);
            nudConcurrentDownloads.TabIndex = 5;
            nudConcurrentDownloads.Value = new decimal(new int[] { 3, 0, 0, 0 });
            // 
            // lblSpeedLimit
            // 
            lblSpeedLimit.AutoSize = true;
            lblSpeedLimit.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblSpeedLimit.Location = new Point(30, 210);
            lblSpeedLimit.Name = "lblSpeedLimit";
            lblSpeedLimit.Size = new Size(180, 19);
            lblSpeedLimit.TabIndex = 6;
            lblSpeedLimit.Text = "Tốc độ tải tối đa:";
            // 
            // cmbSpeedLimit
            // 
            cmbSpeedLimit.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSpeedLimit.FormattingEnabled = true;
            cmbSpeedLimit.Items.AddRange(new object[] { "1", "5", "10" });
            cmbSpeedLimit.Location = new Point(250, 208);
            cmbSpeedLimit.Name = "cmbSpeedLimit";
            cmbSpeedLimit.Size = new Size(80, 25);
            cmbSpeedLimit.TabIndex = 7;
            // 
            // lblSpeedUnit
            // 
            lblSpeedUnit.AutoSize = true;
            lblSpeedUnit.Font = new Font("Segoe UI", 10F);
            lblSpeedUnit.Location = new Point(340, 210);
            lblSpeedUnit.Name = "lblSpeedUnit";
            lblSpeedUnit.Size = new Size(55, 19);
            lblSpeedUnit.TabIndex = 8;
            lblSpeedUnit.Text = "MB/s";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1184, 661);
            Controls.Add(pnlContent);
            Controls.Add(pnlSidebar);
            Font = new Font("Segoe UI", 9.75F);
            MinimumSize = new Size(1000, 600);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "UDM11 - Multi-File Downloader";
            WindowState = FormWindowState.Maximized;
            pnlSidebar.ResumeLayout(false);
            pnlContent.ResumeLayout(false);
            pnlDownloadPage.ResumeLayout(false);
            pnlDownloadPage.PerformLayout();
            gbConnection.ResumeLayout(false);
            gbConnection.PerformLayout();
            gbServerFiles.ResumeLayout(false);
            gbServerFiles.PerformLayout();
            pnlServerToolbar.ResumeLayout(false);
            pnlServerToolbar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvServer).EndInit();
            gbDownloads.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvDownload).EndInit();
            pnlManageFilePage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvManageFile).EndInit();
            pnlManageFileToolbar.ResumeLayout(false);
            pnlManageFileToolbar.PerformLayout();
            pnlSettingsPage.ResumeLayout(false);
            pnlSettingsPage.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlSidebar;
        private Label lblSidebarTitle;
        private Button btnNavManageFile;
        private Button btnNavDownload;
        private Button btnNavSetting;
        private Panel pnlContent;
        private Panel pnlDownloadPage;
        private Panel pnlManageFilePage;
        private GroupBox gbConnection;
        private Label lblStatus;
        private Button btnShowConnectDialog;
        private Label lblConnectedInfo;
        private GroupBox gbServerFiles;
        private Panel pnlServerToolbar;
        private GroupBox gbDownloads;
        private DataGridView dgvServer;
        private Button btnRefreshServerList;
        private DataGridView dgvDownload;
        private Label lblSaveFolder;
        private TextBox txtSaveFolder;
        private Button btnChooseFolder;
        private Label lblLastSaved;
        private Label lblDownloadStats;
        private TextBox txtNotification;
        private DataGridView dgvManageFile;
        private Label lblManageFileTitle;
        private Panel pnlManageFileToolbar;
        private TextBox txtSearchBox;
        private Button btnRefresh;
        private Button btnRename;
        private Button btnDeleteFile;
        private Button btnUpload;
        private Panel pnlSettingsPage;
        private Label lblSettingsTitle;
        private Label lblSettingsUsername;
        private Label lblSettingsUsernameValue;
        private Button btnChangePassword;
        private Label lblConcurrentDownloads;
        private NumericUpDown nudConcurrentDownloads;
        private Label lblSpeedLimit;
        private ComboBox cmbSpeedLimit;
        private Label lblSpeedUnit;
    }
}
