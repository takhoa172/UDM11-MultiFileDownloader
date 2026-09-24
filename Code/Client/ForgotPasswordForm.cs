using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Shared;

namespace Client
{
    public partial class ForgotPasswordForm : Form
    {
        private readonly NetworkService _networkService;
        private string _verifiedUsername = "";

        public ForgotPasswordForm(NetworkService networkService)
        {
            InitializeComponent();
            _networkService = networkService;
            txtUsername.Focus();
        }

        private async void btnSubmit_Click(object? sender, EventArgs e)
        {
            lblError.Text = "";

            string username = txtUsername.Text.Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                lblError.Text = "Vui lòng nhập tên đăng nhập.";
                txtUsername.Focus();
                return;
            }

            btnSubmit.Enabled = false;
            btnSubmit.Text = "Đang kiểm tra...";

            try
            {
                ProtocolPacket response = await _networkService.RequestAsync(new ProtocolPacket
                {
                    Command = PacketCommand.CHECK_USER,
                    Username = username
                });

                if (response.Command != PacketCommand.AUTH_RESP || !response.Success)
                {
                    lblError.Text = response.Message ?? "Tài khoản không tồn tại.";
                    txtUsername.Focus();
                    return;
                }

                _verifiedUsername = username;
                ShowStep2();
            }
            catch (SocketException)
            {
                lblError.Text = "Mất kết nối máy chủ.";
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
                btnSubmit.Enabled = true;
                btnSubmit.Text = "Xác nhận";
            }
        }

        private void ShowStep2()
        {
            lblUsername.Visible = false;
            txtUsername.Visible = false;
            btnSubmit.Visible = false;

            lblVerifiedUser.Text = $"Tài khoản: {_verifiedUsername}";
            lblVerifiedUser.Visible = true;
            lblNewPassword.Visible = true;
            txtNewPassword.Visible = true;
            lblConfirmPassword.Visible = true;
            txtConfirmPassword.Visible = true;
            btnReset.Visible = true;

            lblError.Location = new Point(31, 195);
            btnReset.Location = new Point(31, 222);
            btnCancel.Location = new Point(196, 222);
            ClientSize = new Size(380, 275);
            AcceptButton = btnReset;

            txtNewPassword.Focus();
        }

        private async void btnReset_Click(object? sender, EventArgs e)
        {

            string newPassword = txtNewPassword.Text;
            string confirmPassword = txtConfirmPassword.Text;

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                txtNewPassword.Focus();
                return;
            }

            if (newPassword != confirmPassword)
            {
                lblError.Text = "Mật khẩu xác nhận không khớp.";
                txtConfirmPassword.Focus();
                return;
            }

            try
            {
                btnReset.Enabled = false;
                btnReset.Text = "Đang đặt lại...";

                ProtocolPacket response = await _networkService.RequestAsync(new ProtocolPacket
                {
                    Command = PacketCommand.RESET_PASSWORD,
                    Username = _verifiedUsername,
                    NewPasswordHash = HashHelper.CalculateSha256(Encoding.UTF8.GetBytes(newPassword))
                });

                if (response.Command == PacketCommand.AUTH_RESP && response.Success)
                {
                    MessageBox.Show(response.Message ?? "Đặt lại mật khẩu thành công!",
                        "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    lblError.Text = response.Message ?? "Đặt lại mật khẩu thất bại.";
                }
            }
            catch (SocketException)
            {
                lblError.Text = "Mất kết nối máy chủ.";
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
                btnReset.Enabled = true;
                btnReset.Text = "Đặt lại";
            }
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
