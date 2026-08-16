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
            txtServerPort = new TextBox();
            txtServerIp = new TextBox();
            lblStatus = new Label();
            btnConnect = new Button();
            lblPort = new Label();
            lblIp = new Label();
            gbServerFiles = new GroupBox();
            listView1 = new ListView();
            columnHeader1 = new ColumnHeader();
            columnHeader2 = new ColumnHeader();
            gbDownloads = new GroupBox();
            listView2 = new ListView();
            columnHeader3 = new ColumnHeader();
            columnHeader4 = new ColumnHeader();
            columnHeader5 = new ColumnHeader();
            gbConnection.SuspendLayout();
            gbServerFiles.SuspendLayout();
            gbDownloads.SuspendLayout();
            SuspendLayout();
            // 
            // gbConnection
            // 
            gbConnection.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            gbConnection.Controls.Add(txtServerPort);
            gbConnection.Controls.Add(txtServerIp);
            gbConnection.Controls.Add(lblStatus);
            gbConnection.Controls.Add(btnConnect);
            gbConnection.Controls.Add(lblPort);
            gbConnection.Controls.Add(lblIp);
            gbConnection.Location = new Point(12, 12);
            gbConnection.Name = "gbConnection";
            gbConnection.Size = new Size(880, 80);
            gbConnection.TabIndex = 0;
            gbConnection.TabStop = false;
            gbConnection.Text = "Cấu hình kết nối Server";
            // 
            // txtServerPort
            // 
            txtServerPort.Location = new Point(260, 25);
            txtServerPort.Name = "txtServerPort";
            txtServerPort.Size = new Size(60, 25);
            txtServerPort.TabIndex = 5;
            txtServerPort.Text = "8080";
            // 
            // txtServerIp
            // 
            txtServerIp.Location = new Point(85, 25);
            txtServerIp.Name = "txtServerIp";
            txtServerIp.Size = new Size(120, 25);
            txtServerIp.TabIndex = 4;
            txtServerIp.Text = "127.0.0.1";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            lblStatus.ForeColor = Color.Red;
            lblStatus.Location = new Point(450, 28);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(99, 17);
            lblStatus.TabIndex = 3;
            lblStatus.Text = "● Chưa kết nối";
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(340, 23);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(90, 30);
            btnConnect.TabIndex = 2;
            btnConnect.Text = "Kết nối";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // lblPort
            // 
            lblPort.AutoSize = true;
            lblPort.Location = new Point(220, 28);
            lblPort.Name = "lblPort";
            lblPort.Size = new Size(39, 17);
            lblPort.TabIndex = 1;
            lblPort.Text = "Port :";
            // 
            // lblIp
            // 
            lblIp.AutoSize = true;
            lblIp.Location = new Point(15, 28);
            lblIp.Name = "lblIp";
            lblIp.Size = new Size(62, 17);
            lblIp.TabIndex = 0;
            lblIp.Text = "IP Server:";
            // 
            // gbServerFiles
            // 
            gbServerFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            gbServerFiles.Controls.Add(listView1);
            gbServerFiles.Location = new Point(12, 98);
            gbServerFiles.Name = "gbServerFiles";
            gbServerFiles.Size = new Size(394, 387);
            gbServerFiles.TabIndex = 1;
            gbServerFiles.TabStop = false;
            gbServerFiles.Text = "📁 Danh sách File trên Server";
            // 
            // listView1
            // 
            listView1.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2 });
            listView1.Dock = DockStyle.Fill;
            listView1.Location = new Point(3, 21);
            listView1.Name = "listView1";
            listView1.Size = new Size(388, 363);
            listView1.TabIndex = 0;
            listView1.UseCompatibleStateImageBehavior = false;
            listView1.View = View.Details;
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
            gbDownloads.Controls.Add(listView2);
            gbDownloads.Location = new Point(412, 98);
            gbDownloads.Name = "gbDownloads";
            gbDownloads.Size = new Size(480, 387);
            gbDownloads.TabIndex = 2;
            gbDownloads.TabStop = false;
            gbDownloads.Text = "📥 Khu vực Download (Kéo thả file vào đây)";
            // 
            // listView2
            // 
            listView2.AllowDrop = true;
            listView2.Columns.AddRange(new ColumnHeader[] { columnHeader3, columnHeader4, columnHeader5 });
            listView2.Dock = DockStyle.Fill;
            listView2.FullRowSelect = true;
            listView2.Location = new Point(3, 21);
            listView2.Name = "listView2";
            listView2.Size = new Size(474, 363);
            listView2.TabIndex = 0;
            listView2.UseCompatibleStateImageBehavior = false;
            listView2.View = View.Details;
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
            columnHeader5.Width = 150;
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
            ResumeLayout(false);
        }

        #endregion

        private GroupBox gbConnection;
        private TextBox txtServerPort;
        private TextBox txtServerIp;
        private Label lblStatus;
        private Button btnConnect;
        private Label lblPort;
        private Label lblIp;
        private GroupBox gbServerFiles;
        private GroupBox gbDownloads;
        private ListView listView1;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private ListView listView2;
        private ColumnHeader columnHeader3;
        private ColumnHeader columnHeader4;
        private ColumnHeader columnHeader5;
    }
}