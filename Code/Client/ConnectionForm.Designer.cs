namespace Client
{
    partial class ConnectionForm
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
            lblTitle = new Label();
            lblIp = new Label();
            lblPort = new Label();
            txtIp = new TextBox();
            txtPort = new TextBox();
            btnConnect = new Button();
            btnCancel = new Button();
            lblError = new Label();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblTitle.Location = new Point(20, 15);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(360, 25);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Kết nối đến máy chủ";
            // 
            // lblIp
            // 
            lblIp.AutoSize = true;
            lblIp.Location = new Point(30, 60);
            lblIp.Name = "lblIp";
            lblIp.Size = new Size(62, 17);
            lblIp.TabIndex = 1;
            lblIp.Text = "IP máy chủ:";
            // 
            // txtIp
            // 
            txtIp.Location = new Point(110, 57);
            txtIp.Name = "txtIp";
            txtIp.Size = new Size(150, 25);
            txtIp.TabIndex = 2;
            txtIp.Text = "127.0.0.1";
            // 
            // lblPort
            // 
            lblPort.AutoSize = true;
            lblPort.Location = new Point(280, 60);
            lblPort.Name = "lblPort";
            lblPort.Size = new Size(35, 17);
            lblPort.TabIndex = 3;
            lblPort.Text = "Cổng:";
            // 
            // txtPort
            // 
            txtPort.Location = new Point(320, 57);
            txtPort.Name = "txtPort";
            txtPort.Size = new Size(60, 25);
            txtPort.TabIndex = 4;
            txtPort.Text = "8080";
            // 
            // lblError
            // 
            lblError.ForeColor = Color.Red;
            lblError.Location = new Point(30, 90);
            lblError.Name = "lblError";
            lblError.Size = new Size(350, 20);
            lblError.TabIndex = 5;
            lblError.Text = "";
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(200, 120);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(90, 32);
            btnConnect.TabIndex = 6;
            btnConnect.Text = "Kết nối";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(300, 120);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(80, 32);
            btnCancel.TabIndex = 7;
            btnCancel.Text = "Hủy";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // ConnectionForm
            // 
            AcceptButton = btnConnect;
            CancelButton = btnCancel;
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(410, 165);
            Controls.Add(lblTitle);
            Controls.Add(lblIp);
            Controls.Add(txtIp);
            Controls.Add(lblPort);
            Controls.Add(txtPort);
            Controls.Add(lblError);
            Controls.Add(btnConnect);
            Controls.Add(btnCancel);
            Font = new Font("Segoe UI", 9.75F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ConnectionForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Kết nối máy chủ";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblTitle;
        private Label lblIp;
        private Label lblPort;
        private TextBox txtIp;
        private TextBox txtPort;
        private Button btnConnect;
        private Button btnCancel;
        private Label lblError;
    }
}
