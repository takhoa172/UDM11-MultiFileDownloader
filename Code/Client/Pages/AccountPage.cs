using System;
using System.Windows.Forms;
using Shared;

namespace Client.Pages
{
    public class AccountPage : UserControl
    {
        // Khai báo giao diện 
        private TextBox txtCurrentPassword = new TextBox();
        private TextBox txtNewPassword = new TextBox();
        private TextBox txtConfirmPassword = new TextBox();
        private Button btnChangePassword = new Button();

        public AccountPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            Label lblCurrent = new Label() { Text = "Mật khẩu hiện tại:", Top = 20, Left = 20, Width = 150 };
            txtCurrentPassword.Top = 20; txtCurrentPassword.Left = 180; txtCurrentPassword.UseSystemPasswordChar = true;

            Label lblNew = new Label() { Text = "Mật khẩu mới:", Top = 60, Left = 20, Width = 150 };
            txtNewPassword.Top = 60; txtNewPassword.Left = 180; txtNewPassword.UseSystemPasswordChar = true;

            Label lblConfirm = new Label() { Text = "Xác nhận mật khẩu:", Top = 100, Left = 20, Width = 150 };
            txtConfirmPassword.Top = 100; txtConfirmPassword.Left = 180; txtConfirmPassword.UseSystemPasswordChar = true;

            btnChangePassword.Text = "Đổi Mật Khẩu";
            btnChangePassword.Top = 140; btnChangePassword.Left = 180; btnChangePassword.Width = 100;
            btnChangePassword.Click += btnChangePassword_Click;

            this.Controls.Add(lblCurrent);
            this.Controls.Add(txtCurrentPassword);
            this.Controls.Add(lblNew);
            this.Controls.Add(txtNewPassword);
            this.Controls.Add(lblConfirm);
            this.Controls.Add(txtConfirmPassword);
            this.Controls.Add(btnChangePassword);

            this.ResumeLayout(false);
        }

        private void btnChangePassword_Click(object? sender, EventArgs e)
        {
            string currentPass = txtCurrentPassword.Text;
            string newPass = txtNewPassword.Text;
            string confirmPass = txtConfirmPassword.Text;

            if (string.IsNullOrEmpty(currentPass) || string.IsNullOrEmpty(newPass))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin mật khẩu.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (newPass != confirmPass)
            {
                MessageBox.Show("Mật khẩu xác nhận không khớp!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                ProtocolPacket changePassPacket = new ProtocolPacket
                {
                    Command = PacketCommand.CHANGE_PASSWORD,
                    PasswordHash = currentPass,
                    NewPasswordHash = newPass
                };

                string packetData = PacketHelper.EncodeToString(changePassPacket);
                MessageBox.Show("Đã gửi yêu cầu đổi mật khẩu lên Server.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                txtCurrentPassword.Clear();
                txtNewPassword.Clear();
                txtConfirmPassword.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi gửi yêu cầu: {ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}