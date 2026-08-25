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
            lblStatus = new Label();
            lblPort = new Label();
            lblIP = new Label();
            gbServerFiles = new GroupBox();
            gbDownloads = new GroupBox();
            dgvDownload = new DataGridView();
            dgvServer = new DataGridView();
            gbConnection.SuspendLayout();
            gbServerFiles.SuspendLayout();
            gbDownloads.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvDownload).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvServer).BeginInit();
            SuspendLayout();
            // 
            // gbConnection
            // 
            gbConnection.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            gbConnection.Controls.Add(lblStatus);
            gbConnection.Controls.Add(lblPort);
            gbConnection.Controls.Add(lblIP);
            gbConnection.Location = new Point(12, 12);
            gbConnection.Name = "gbConnection";
            gbConnection.Size = new Size(880, 80);
            gbConnection.TabIndex = 0;
            gbConnection.TabStop = false;
            gbConnection.Text = "Cấu hình kết nối Server";
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
            // lblPort
            // 
            lblPort.AutoSize = true;
            lblPort.Location = new Point(200, 30);
            lblPort.Name = "lblPort";
            lblPort.Size = new Size(67, 17);
            lblPort.TabIndex = 1;
            lblPort.Text = "Port: 8080";
            // 
            // lblIP
            // 
            lblIP.AutoSize = true;
            lblIP.Location = new Point(20, 30);
            lblIP.Name = "lblIP";
            lblIP.Size = new Size(117, 17);
            lblIP.TabIndex = 0;
            lblIP.Text = "IP Server: 127.0.0.1";
            // 
            // gbServerFiles
            // 
            gbServerFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            gbServerFiles.Controls.Add(dgvServer);
            gbServerFiles.Location = new Point(12, 98);
            gbServerFiles.Name = "gbServerFiles";
            gbServerFiles.Size = new Size(394, 387);
            gbServerFiles.TabIndex = 1;
            gbServerFiles.TabStop = false;
            gbServerFiles.Text = "📁 Danh sách File trên Server";
            // 
            // gbDownloads
            // 
            gbDownloads.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            gbDownloads.Controls.Add(dgvDownload);
            gbDownloads.Location = new Point(412, 98);
            gbDownloads.Name = "gbDownloads";
            gbDownloads.Size = new Size(480, 387);
            gbDownloads.TabIndex = 2;
            gbDownloads.TabStop = false;
            gbDownloads.Text = "📥 Khu vực Download (Kéo thả file vào đây)";
            // 
            // dgvDownload
            // 
            dgvDownload.AllowDrop = true;
            dgvDownload.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvDownload.Dock = DockStyle.Fill;
            dgvDownload.Location = new Point(3, 21);
            dgvDownload.Name = "dgvDownload";
            dgvDownload.ReadOnly = true;
            dgvDownload.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDownload.Size = new Size(474, 363);
            dgvDownload.TabIndex = 0;
            // 
            // dgvServer
            // 
            dgvServer.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvServer.Dock = DockStyle.Fill;
            dgvServer.Location = new Point(3, 21);
            dgvServer.Name = "dgvServer";
            dgvServer.ReadOnly = true;
            dgvServer.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvServer.Size = new Size(388, 363);
            dgvServer.TabIndex = 0;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(904, 590);
            Controls.Add(gbDownloads);
            Controls.Add(gbServerFiles);
            Controls.Add(gbConnection);
            Font = new Font("Segoe UI", 9.75F);
            MinimumSize = new Size(920, 629);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "UDM11 - Multi-File Downloader";
            gbConnection.ResumeLayout(false);
            gbConnection.PerformLayout();
            gbServerFiles.ResumeLayout(false);
            gbDownloads.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvDownload).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvServer).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox gbConnection;
        private TextBox txtServerPort;
        private TextBox txtServerIp;
        private Label lblStatus;
        private Button btnConnect;
        private Label lblPort;
        private Label lblIP;
        private GroupBox gbServerFiles;
        private GroupBox gbDownloads;
        private DataGridView dgvServer;
        private DataGridView dgvDownload;
    }
}