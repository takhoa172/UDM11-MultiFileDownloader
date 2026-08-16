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
            txtServerPort = new TextBox();
            txtServerIp = new TextBox();
            lblPort = new Label();
            lblStatus = new Label();
            lblIp = new Label();
            gbServerFiles = new GroupBox();
            lvServerFiles = new ListView();
            columnHeader1 = new ColumnHeader();
            columnHeader2 = new ColumnHeader();
            gbDownloads = new GroupBox();
            lvDownloads = new ListView();
            columnHeader3 = new ColumnHeader();
            columnHeader4 = new ColumnHeader();
            columnHeader5 = new ColumnHeader();
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            gbConnection.SuspendLayout();
            gbServerFiles.SuspendLayout();
            gbDownloads.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // gbConnection
            // 
            gbConnection.Controls.Add(btnConnect);
            gbConnection.Controls.Add(txtServerPort);
            gbConnection.Controls.Add(txtServerIp);
            gbConnection.Controls.Add(lblPort);
            gbConnection.Controls.Add(lblStatus);
            gbConnection.Controls.Add(lblIp);
            gbConnection.Dock = DockStyle.Top;
            gbConnection.Location = new Point(0, 0);
            gbConnection.Margin = new Padding(4, 3, 4, 3);
            gbConnection.Name = "gbConnection";
            gbConnection.Padding = new Padding(4, 3, 4, 3);
            gbConnection.Size = new Size(884, 65);
            gbConnection.TabIndex = 0;
            gbConnection.TabStop = false;
            gbConnection.Text = "Cấu hình kết nối Server";
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(340, 23);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(90, 30);
            btnConnect.TabIndex = 0;
            btnConnect.Text = "Kết nối";
            btnConnect.Click += btnConnect_Click;
            // 
            // txtServerPort
            // 
            txtServerPort.Location = new Point(260, 25);
            txtServerPort.Name = "txtServerPort";
            txtServerPort.Size = new Size(60, 25);
            txtServerPort.TabIndex = 4;
            txtServerPort.Text = "8080";
            // 
            // txtServerIp
            // 
            txtServerIp.Location = new Point(85, 25);
            txtServerIp.Name = "txtServerIp";
            txtServerIp.Size = new Size(120, 25);
            txtServerIp.TabIndex = 3;
            txtServerIp.Text = "127.0.0.1";
            // 
            // lblPort
            // 
            lblPort.AutoSize = true;
            lblPort.Location = new Point(220, 28);
            lblPort.Margin = new Padding(4, 0, 4, 0);
            lblPort.Name = "lblPort";
            lblPort.Size = new Size(35, 17);
            lblPort.TabIndex = 2;
            lblPort.Text = "Port:";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblStatus.ForeColor = Color.Red;
            lblStatus.Location = new Point(450, 28);
            lblStatus.Margin = new Padding(4, 0, 4, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(105, 19);
            lblStatus.TabIndex = 1;
            lblStatus.Text = "● Chưa kết nối";
            // 
            // lblIp
            // 
            lblIp.AutoSize = true;
            lblIp.Location = new Point(15, 28);
            lblIp.Margin = new Padding(4, 0, 4, 0);
            lblIp.Name = "lblIp";
            lblIp.Size = new Size(62, 17);
            lblIp.TabIndex = 0;
            lblIp.Text = "IP Server:";
            // 
            // gbServerFiles
            // 
            gbServerFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            gbServerFiles.Controls.Add(lvServerFiles);
            gbServerFiles.Location = new Point(12, 85);
            gbServerFiles.Name = "gbServerFiles";
            gbServerFiles.Size = new Size(390, 382);
            gbServerFiles.TabIndex = 1;
            gbServerFiles.TabStop = false;
            gbServerFiles.Text = "📁 Danh sách File trên Server";
            // 
            // lvServerFiles
            // 
            lvServerFiles.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2 });
            lvServerFiles.Dock = DockStyle.Fill;
            lvServerFiles.FullRowSelect = true;
            lvServerFiles.Location = new Point(3, 21);
            lvServerFiles.Name = "lvServerFiles";
            lvServerFiles.Size = new Size(384, 358);
            lvServerFiles.TabIndex = 0;
            lvServerFiles.UseCompatibleStateImageBehavior = false;
            lvServerFiles.View = View.Details;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "Tên File";
            columnHeader1.Width = 250;
            // 
            // columnHeader2
            // 
            columnHeader2.Text = "Kích thước";
            columnHeader2.Width = 110;
            // 
            // gbDownloads
            // 
            gbDownloads.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            gbDownloads.Controls.Add(lvDownloads);
            gbDownloads.Location = new Point(412, 85);
            gbDownloads.Name = "gbDownloads";
            gbDownloads.Size = new Size(430, 382);
            gbDownloads.TabIndex = 2;
            gbDownloads.TabStop = false;
            gbDownloads.Text = "📥 Khu vực Download (Kéo thả file vào đây)";
            // 
            // lvDownloads
            // 
            lvDownloads.AllowDrop = true;
            lvDownloads.Columns.AddRange(new ColumnHeader[] { columnHeader3, columnHeader4, columnHeader5 });
            lvDownloads.Dock = DockStyle.Fill;
            lvDownloads.FullRowSelect = true;
            lvDownloads.Location = new Point(3, 21);
            lvDownloads.Name = "lvDownloads";
            lvDownloads.Size = new Size(424, 358);
            lvDownloads.TabIndex = 0;
            lvDownloads.UseCompatibleStateImageBehavior = false;
            lvDownloads.View = View.Details;
            // 
            // columnHeader3
            // 
            columnHeader3.Text = "Tên File";
            columnHeader3.Width = 180;
            // 
            // columnHeader4
            // 
            columnHeader4.Text = "Tiến độ";
            columnHeader4.Width = 120;
            // 
            // columnHeader5
            // 
            columnHeader5.Text = "Trạng thái";
            columnHeader5.Width = 120;
            // 
            // statusStrip1
            // 
            statusStrip1.Dock = DockStyle.None;
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1 });
            statusStrip1.Location = new Point(104, 481);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(622, 22);
            statusStrip1.TabIndex = 3;
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.ForeColor = Color.DimGray;
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(574, 17);
            toolStripStatusLabel1.Text = "💡 Hướng dẫn: Chọn 1 hoặc nhiều file bên bảng Server, giữ chuột và kéo thả sang bảng Download để tải về.";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(884, 561);
            Controls.Add(statusStrip1);
            Controls.Add(gbDownloads);
            Controls.Add(gbServerFiles);
            Controls.Add(gbConnection);
            Font = new Font("Segoe UI", 9.75F);
            Margin = new Padding(4, 3, 4, 3);
            MinimumSize = new Size(900, 600);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Multi-File Downloader (Client)";
            gbConnection.ResumeLayout(false);
            gbConnection.PerformLayout();
            gbServerFiles.ResumeLayout(false);
            gbDownloads.ResumeLayout(false);
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private GroupBox gbConnection;
        private Label lblPort;
        private Label lblStatus;
        private Label lblIp;
        private TextBox txtServerPort;
        private TextBox txtServerIp;
        private GroupBox gbServerFiles;
        private GroupBox gbDownloads;
        private ListView lvServerFiles;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private ListView lvDownloads;
        private ColumnHeader columnHeader3;
        private ColumnHeader columnHeader4;
        private ColumnHeader columnHeader5;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private Button btnConnect;
    }
}