using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Shared;

namespace Client
{
    public partial class RegisterForm : Form
    {
        private readonly NetworkService _networkService;

        public RegisterForm(NetworkService networkService)
        {
            InitializeComponent();
            _networkService = networkService;
            txtUsername.Focus();
        }

        private async void btnRegister_Click(object? sender, EventArgs e)
        {
            lblError.Text = "";

            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;
            string confirmPassword = txtConfirmPassword.Text;

            if (string.IsNullOrWhiteSpace(username))
            {
                lblError.Text = "Vui lòng nhập tên đăng nhập.";
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                lblError.Text = "Mật khẩu phải từ 6 ký tự trở lên.";
                txtPassword.Focus();
                return;
            }

            if (password != confirmPassword)
            {
                lblError.Text = "Mật khẩu xác nhận không khớp.";
                txtConfirmPassword.Focus();
                return;
            }

            try
            {
                btnRegister.Enabled = false;
                btnRegister.Text = "Đăng ký...";

                ProtocolPacket response = await _networkService.RequestAsync(new ProtocolPacket
                {
                    Command = PacketCommand.REGISTER,
                    Username = username,
                    PasswordHash = HashHelper.CalculateSha256(Encoding.UTF8.GetBytes(password))
                });

                if (response.Command == PacketCommand.AUTH_RESP && response.Success)
                {
                    MessageBox.Show(response.Message ?? "Đăng ký thành công!",
                        "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    lblError.Text = response.Message ?? "Đăng ký thất bại.";
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
                btnRegister.Enabled = true;
                btnRegister.Text = "Đăng ký";
            }
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
