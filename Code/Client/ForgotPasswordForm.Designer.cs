namespace Client
{
    partial class ForgotPasswordForm
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
            lblUsername = new Label();
            txtUsername = new TextBox();
            btnSubmit = new Button();
            btnCancel = new Button();
            lblError = new Label();
            lblVerifiedUser = new Label();
            lblNewPassword = new Label();
            txtNewPassword = new TextBox();
            lblConfirmPassword = new Label();
            txtConfirmPassword = new TextBox();
            btnReset = new Button();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblTitle.Location = new Point(2, 23);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(380, 30);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Quên mật khẩu";
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.Location = new Point(31, 80);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(98, 17);
            lblUsername.TabIndex = 2;
            lblUsername.Text = "Tên đăng nhập:";
            // 
            // txtUsername
            // 
            txtUsername.Location = new Point(31, 108);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(320, 25);
            txtUsername.TabIndex = 3;
            // 
            // btnSubmit
            // 
            btnSubmit.Location = new Point(31, 167);
            btnSubmit.Name = "btnSubmit";
            btnSubmit.Size = new Size(155, 35);
            btnSubmit.TabIndex = 5;
            btnSubmit.Text = "Xác nhận";
            btnSubmit.UseVisualStyleBackColor = true;
            btnSubmit.Click += btnSubmit_Click;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(196, 167);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(155, 35);
            btnCancel.TabIndex = 6;
            btnCancel.Text = "Hủy";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // lblError
            // 
            lblError.ForeColor = Color.Red;
            lblError.Location = new Point(31, 140);
            lblError.Name = "lblError";
            lblError.Size = new Size(320, 20);
            lblError.TabIndex = 7;
            // 
            // lblVerifiedUser
            // 
            lblVerifiedUser.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            lblVerifiedUser.ForeColor = Color.ForestGreen;
            lblVerifiedUser.Location = new Point(35, 50);
            lblVerifiedUser.Name = "lblVerifiedUser";
            lblVerifiedUser.Size = new Size(320, 20);
            lblVerifiedUser.TabIndex = 8;
            lblVerifiedUser.Visible = false;
            // 
            // lblNewPassword
            // 
            lblNewPassword.AutoSize = true;
            lblNewPassword.Location = new Point(31, 82);
            lblNewPassword.Name = "lblNewPassword";
            lblNewPassword.Size = new Size(91, 17);
            lblNewPassword.TabIndex = 9;
            lblNewPassword.Text = "Mật khẩu mới:";
            lblNewPassword.Visible = false;
            // 
            // txtNewPassword
            // 
            txtNewPassword.Location = new Point(31, 102);
            txtNewPassword.Name = "txtNewPassword";
            txtNewPassword.Size = new Size(320, 25);
            txtNewPassword.TabIndex = 10;
            txtNewPassword.UseSystemPasswordChar = true;
            txtNewPassword.Visible = false;
            // 
            // lblConfirmPassword
            // 
            lblConfirmPassword.AutoSize = true;
            lblConfirmPassword.Location = new Point(31, 137);
            lblConfirmPassword.Name = "lblConfirmPassword";
            lblConfirmPassword.Size = new Size(121, 17);
            lblConfirmPassword.TabIndex = 11;
            lblConfirmPassword.Text = "Xác nhận mật khẩu:";
            lblConfirmPassword.Visible = false;
            // 
            // txtConfirmPassword
            // 
            txtConfirmPassword.Location = new Point(31, 157);
            txtConfirmPassword.Name = "txtConfirmPassword";
            txtConfirmPassword.Size = new Size(320, 25);
            txtConfirmPassword.TabIndex = 12;
            txtConfirmPassword.UseSystemPasswordChar = true;
            txtConfirmPassword.Visible = false;
            // 
            // btnReset
            // 
            btnReset.Location = new Point(31, 222);
            btnReset.Name = "btnReset";
            btnReset.Size = new Size(155, 35);
            btnReset.TabIndex = 13;
            btnReset.Text = "Đặt lại";
            btnReset.UseVisualStyleBackColor = true;
            btnReset.Visible = false;
            btnReset.Click += btnReset_Click;
            // 
            // ForgotPasswordForm
            // 
            AcceptButton = btnSubmit;
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(380, 225);
            Controls.Add(lblTitle);
            Controls.Add(lblUsername);
            Controls.Add(txtUsername);
            Controls.Add(btnSubmit);
            Controls.Add(btnCancel);
            Controls.Add(lblVerifiedUser);
            Controls.Add(lblNewPassword);
            Controls.Add(txtNewPassword);
            Controls.Add(lblConfirmPassword);
            Controls.Add(txtConfirmPassword);
            Controls.Add(btnReset);
            Controls.Add(lblError);
            Font = new Font("Segoe UI", 9.75F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ForgotPasswordForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Quên mật khẩu";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblTitle;
        private Label lblUsername;
        private TextBox txtUsername;
        private Button btnSubmit;
        private Button btnCancel;
        private Label lblError;
        private Label lblVerifiedUser;
        private Label lblNewPassword;
        private TextBox txtNewPassword;
        private Label lblConfirmPassword;
        private TextBox txtConfirmPassword;
        private Button btnReset;
    }
}
