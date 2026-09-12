namespace Client
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            gbConnection = new GroupBox();
            btnConnect = new Button();
            txtServerIp = new TextBox();
            txtServerPort = new TextBox();
            lblServerPort = new Label();
            lblStatus = new Label();
            lblServerIp = new Label();
            lblSaveFolder = new Label();
            txtSaveFolder = new TextBox();
            btnChooseFolder = new Button();
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
            SuspendLayout();
            // 
            // gbConnection
            // 
            gbConnection.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            gbConnection.Controls.Add(btnConnect);
            gbConnection.Controls.Add(txtServerIp);
            gbConnection.Controls.Add(txtServerPort);
            gbConnection.Controls.Add(lblServerPort);
            gbConnection.Controls.Add(lblStatus);
            gbConnection.Controls.Add(lblServerIp);
            gbConnection.Controls.Add(lblSaveFolder);
            gbConnection.Controls.Add(txtSaveFolder);
            gbConnection.Controls.Add(btnChooseFolder);
            gbConnection.Location = new Point(12, 12);
            gbConnection.Name = "gbConnection";
            gbConnection.Size = new Size(960, 80);
            gbConnection.TabIndex = 0;
            gbConnection.TabStop = false;
            gbConnection.Text = "Cấu hình kết nối Server";
            // 
            // btnConnect
            // 
            btnConnect.AutoSize = true;
            btnConnect.Location = new Point(350, 26);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(85, 27);
            btnConnect.TabIndex = 7;
            btnConnect.Text = "Kết nối";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // txtServerIp
            // 
            txtServerIp.Location = new Point(95, 27);
            txtServerIp.Name = "txtServerIp";
            txtServerIp.Size = new Size(120, 25);
            txtServerIp.TabIndex = 6;
            txtServerIp.Text = "127.0.0.1";
            // 
            // txtServerPort
            // 
            txtServerPort.Location = new Point(270, 27);
            txtServerPort.Name = "txtServerPort";
            txtServerPort.Size = new Size(60, 25);
            txtServerPort.TabIndex = 5;
            txtServerPort.Text = "8080";
            // 
            // lblServerPort
            // 
            lblServerPort.AutoSize = true;
            lblServerPort.Location = new Point(230, 30);
            lblServerPort.Name = "lblServerPort";
            lblServerPort.Size = new Size(35, 17);
            lblServerPort.TabIndex = 4;
            lblServerPort.Text = "Port:";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            lblStatus.ForeColor = Color.Red;
            lblStatus.Location = new Point(450, 30);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(99, 17);
            lblStatus.TabIndex = 3;
            lblStatus.Text = "● Chưa kết nối";
            // 
            // lblServerIp
            // 
            lblServerIp.AutoSize = true;
            lblServerIp.Location = new Point(25, 30);
            lblServerIp.Name = "lblServerIp";
            lblServerIp.Size = new Size(62, 17);
            lblServerIp.TabIndex = 0;
            lblServerIp.Text = "IP Server:";
            // 
            // lblSaveFolder
            // 
            lblSaveFolder.AutoSize = true;
            lblSaveFolder.Location = new Point(560, 28);
            lblSaveFolder.Name = "lblSaveFolder";
            lblSaveFolder.Size = new Size(87, 17);
            lblSaveFolder.TabIndex = 8;
            lblSaveFolder.Text = "Thư mục lưu: ";
            // 
            // txtSaveFolder
            // 
            txtSaveFolder.Location = new Point(560, 42);
            txtSaveFolder.Name = "txtSaveFolder";
            txtSaveFolder.ReadOnly = true;
            txtSaveFolder.Size = new Size(300, 25);
            txtSaveFolder.TabIndex = 9;
            txtSaveFolder.TabStop = false;
            // 
            // btnChooseFolder
            // 
            btnChooseFolder.Location = new Point(866, 40);
            btnChooseFolder.Name = "btnChooseFolder";
            btnChooseFolder.Size = new Size(70, 27);
            btnChooseFolder.TabIndex = 10;
            btnChooseFolder.Text = "Chọn...";
            btnChooseFolder.UseVisualStyleBackColor = true;
            btnChooseFolder.Click += btnChooseFolder_Click;
            // 
            // gbServerFiles
            // 
            gbServerFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            gbServerFiles.Controls.Add(dgvServer);
            gbServerFiles.Location = new Point(12, 98);
            gbServerFiles.Name = "gbServerFiles";
            gbServerFiles.Size = new Size(394, 358);
            gbServerFiles.TabIndex = 1;
            gbServerFiles.TabStop = false;
            gbServerFiles.Text = "📁 Danh sách File trên Server";
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
            dgvServer.Size = new Size(388, 334);
            dgvServer.TabIndex = 0;
            //dgvServer.CellContentClick += dgvServer_CellContentClick;
            // 
            // gbDownloads
            // 
            gbDownloads.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            gbDownloads.Controls.Add(dgvDownload);
            gbDownloads.Location = new Point(412, 98);
            gbDownloads.Name = "gbDownloads";
            gbDownloads.Size = new Size(560, 358);
            gbDownloads.TabIndex = 2;
            gbDownloads.TabStop = false;
            gbDownloads.Text = "📥 Khu vực Download (Kéo thả file vào đây)";
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
            dgvDownload.Size = new Size(554, 334);
            dgvDownload.TabIndex = 0;
            // 
            // lblLastSaved
            // 
            lblLastSaved.AutoSize = true;
            lblLastSaved.ForeColor = Color.ForestGreen;
            lblLastSaved.Location = new Point(12, 462);
            lblLastSaved.Name = "lblLastSaved";
            lblLastSaved.Size = new Size(154, 17);
            lblLastSaved.TabIndex = 3;
            lblLastSaved.Text = "Đã lưu: (chưa có file nào)";
            // 
            // lblDownloadStats
            // 
            lblDownloadStats.AutoSize = true;
            lblDownloadStats.ForeColor = Color.SteelBlue;
            lblDownloadStats.Location = new Point(200, 462);
            lblDownloadStats.Name = "lblDownloadStats";
            lblDownloadStats.Size = new Size(174, 17);
            lblDownloadStats.TabIndex = 4;
            lblDownloadStats.Text = "Tổng: 0 file | Đã tải: 0 | Lỗi: 0";
            // 
            // txtNotification
            // 
            txtNotification.BackColor = Color.White;
            txtNotification.ForeColor = Color.ForestGreen;
            txtNotification.Location = new Point(12, 482);
            txtNotification.Multiline = true;
            txtNotification.Name = "txtNotification";
            txtNotification.ReadOnly = true;
            txtNotification.ScrollBars = ScrollBars.Vertical;
            txtNotification.Size = new Size(960, 60);
            txtNotification.TabIndex = 5;
            txtNotification.TabStop = false;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(984, 561);
            Controls.Add(gbDownloads);
            Controls.Add(gbServerFiles);
            Controls.Add(gbConnection);
            Controls.Add(lblLastSaved);
            Controls.Add(lblDownloadStats);
            Controls.Add(txtNotification);
            Font = new Font("Segoe UI", 9.75F);
            MinimumSize = new Size(1000, 600);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "UDM11 - Multi-File Downloader";
            gbConnection.ResumeLayout(false);
            gbConnection.PerformLayout();
            gbServerFiles.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvServer).EndInit();
            gbDownloads.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvDownload).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private GroupBox gbConnection;
        private Label lblStatus;
        private GroupBox gbServerFiles;
        private GroupBox gbDownloads;
        private DataGridView dgvServer;
        private DataGridView dgvDownload;
        private Label lblServerPort;
        private Label lblServerIp;
        private Button btnConnect;
        private TextBox txtServerIp;
        private TextBox txtServerPort;
        private Label lblSaveFolder;
        private TextBox txtSaveFolder;
        private Button btnChooseFolder;
        private Label lblLastSaved;
        private Label lblDownloadStats;
        private TextBox txtNotification;
    }
}