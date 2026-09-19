using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using Shared;

namespace Client.Pages
{
    public class AccountPage : UserControl
    {
        private readonly NetworkService _networkService;
        private TextBox txtCurrentPassword = new TextBox();
        private TextBox txtNewPassword = new TextBox();
        private TextBox txtConfirmPassword = new TextBox();
        private Button btnChangePassword = new Button();
        private Label lblStatus = new Label();

        public AccountPage(NetworkService networkService)
        {
            _networkService = networkService;
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
            btnChangePassword.Top = 140; btnChangePassword.Left = 180; btnChangePassword.Width = 120;
            btnChangePassword.Click += btnChangePassword_Click;

            lblStatus.Top = 180; lblStatus.Left = 20; lblStatus.Width = 350;
            lblStatus.ForeColor = System.Drawing.Color.Red;

            this.Controls.Add(lblCurrent);
            this.Controls.Add(txtCurrentPassword);
            this.Controls.Add(lblNew);
            this.Controls.Add(txtNewPassword);
            this.Controls.Add(lblConfirm);
            this.Controls.Add(txtConfirmPassword);
            this.Controls.Add(btnChangePassword);
            this.Controls.Add(lblStatus);

            this.ResumeLayout(false);
        }

        private async void btnChangePassword_Click(object? sender, EventArgs e)
        {
            string currentPass = txtCurrentPassword.Text;
            string newPass = txtNewPassword.Text;
            string confirmPass = txtConfirmPassword.Text;

            if (string.IsNullOrEmpty(currentPass) || string.IsNullOrEmpty(newPass))
            {
                lblStatus.ForeColor = System.Drawing.Color.Red;
                lblStatus.Text = "Vui lòng nhập đầy đủ thông tin mật khẩu.";
                return;
            }

            if (newPass != confirmPass)
            {
                lblStatus.ForeColor = System.Drawing.Color.Red;
                lblStatus.Text = "Mật khẩu xác nhận không khớp!";
                return;
            }

            if (!_networkService.IsConnected)
            {
                lblStatus.ForeColor = System.Drawing.Color.Red;
                lblStatus.Text = "Chưa kết nối Server.";
                return;
            }

            try
            {
                btnChangePassword.Enabled = false;
                btnChangePassword.Text = "Đang gửi...";

                await _networkService.SendPacketAsync(new ProtocolPacket
                {
                    Command = PacketCommand.CHANGE_PASSWORD,
                    Password = currentPass,
                    NewPasswordHash = newPass
                });

                ProtocolPacket response = await _networkService.ReadPacketAsync();

                if (response.Command == PacketCommand.AUTH_RESP && response.Success)
                {
                    lblStatus.ForeColor = System.Drawing.Color.Green;
                    lblStatus.Text = response.Message ?? "Đổi mật khẩu thành công.";
                    txtCurrentPassword.Clear();
                    txtNewPassword.Clear();
                    txtConfirmPassword.Clear();
                }
                else
                {
                    lblStatus.ForeColor = System.Drawing.Color.Red;
                    lblStatus.Text = response.Message ?? "Đổi mật khẩu thất bại.";
                }
            }
            catch (SocketException)
            {
                lblStatus.ForeColor = System.Drawing.Color.Red;
                lblStatus.Text = "Mất kết nối Server.";
            }
            catch (Exception ex)
            {
                lblStatus.ForeColor = System.Drawing.Color.Red;
                lblStatus.Text = $"Lỗi: {ex.Message}";
            }
            finally
            {
                btnChangePassword.Enabled = true;
                btnChangePassword.Text = "Đổi Mật Khẩu";
            }
        }
    }
}