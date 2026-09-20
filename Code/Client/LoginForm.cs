using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Shared;

namespace Client
{
    public partial class LoginForm : Form
    {
        private readonly NetworkService _networkService;
        public string LoggedInUsername { get; private set; } = "";
        public string LoggedInToken { get; private set; } = "";

        public LoginForm(NetworkService networkService)
        {
            InitializeComponent();
            _networkService = networkService;
            txtUsername.Focus();
        }

        private async void btnLogin_Click(object? sender, EventArgs e)
        {
            lblError.Text = "";

            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(username))
            {
                lblError.Text = "Vui lòng nhập tên đăng nhập.";
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                lblError.Text = "Vui lòng nhập mật khẩu.";
                txtPassword.Focus();
                return;
            }

            try
            {
                btnLogin.Enabled = false;
                btnLogin.Text = "Đang đăng nhập...";

                ProtocolPacket response = await _networkService.RequestAsync(new ProtocolPacket
                {
                    Command = PacketCommand.LOGIN,
                    Username = username,
                    PasswordHash = HashHelper.CalculateSha256(Encoding.UTF8.GetBytes(password))
                });

                if (response.Command == PacketCommand.AUTH_RESP && response.Success)
                {
                    LoggedInUsername = username;
                    LoggedInToken = response.Token ?? "";
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    lblError.Text = response.ErrorCode == "409_SESSION_ACTIVE"
                        ? "Tài khoản đang được đăng nhập trên thiết bị khác."
                        : response.Message ?? "Đăng nhập thất bại.";
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
            catch (SocketException)
            {
                lblError.Text = "Mất kết nối Server.";
            }
            catch (OperationCanceledException)
            {
                lblError.Text = "Hết thời gian yêu cầu.";
            }
            catch (Exception ex)
            {
                lblError.Text = $"Lỗi: {ex.Message}";
            }
            finally
            {
                btnLogin.Enabled = true;
                btnLogin.Text = "Đăng nhập";
            }
        }

        private void linkRegister_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            using var form = new RegisterForm(_networkService);
            form.ShowDialog(this);
        }

        private void linkForgotPassword_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            using var form = new ForgotPasswordForm(_networkService);
            form.ShowDialog(this);
        }
    }
}
