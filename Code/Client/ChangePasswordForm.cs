using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Shared;

namespace Client
{
    public class ChangePasswordForm : Form
    {
        private readonly NetworkService _networkService;
        private readonly string _username;

        private readonly TextBox txtOld = new();
        private readonly TextBox txtNew = new();
        private readonly TextBox txtConfirm = new();
        private readonly Label lblError = new();
        private readonly Button btnOk = new();

        public ChangePasswordForm(NetworkService networkService, string username)
        {
            _networkService = networkService;
            _username = username;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Đổi mật khẩu";
            Width = 430;
            Height = 280;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            Label lblOld = new() { Text = "Mật khẩu hiện tại:", Left = 20, Top = 25, Width = 140 };
            txtOld.Left = 170; txtOld.Top = 22; txtOld.Width = 210; txtOld.UseSystemPasswordChar = true;

            Label lblNew = new() { Text = "Mật khẩu mới:", Left = 20, Top = 65, Width = 140 };
            txtNew.Left = 170; txtNew.Top = 62; txtNew.Width = 210; txtNew.UseSystemPasswordChar = true;

            Label lblConfirm = new() { Text = "Xác nhận mật khẩu:", Left = 20, Top = 105, Width = 140 };
            txtConfirm.Left = 170; txtConfirm.Top = 102; txtConfirm.Width = 210; txtConfirm.UseSystemPasswordChar = true;

            lblError.Left = 20; lblError.Top = 140; lblError.Width = 380;
            lblError.ForeColor = System.Drawing.Color.Red;

            btnOk.Text = "Đổi mật khẩu";
            btnOk.Left = 170; btnOk.Top = 175; btnOk.Width = 110;
            btnOk.Click += btnOk_Click;

            Button btnCancel = new() { Text = "Hủy", Left = 290, Top = 175, Width = 90 };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[]
            {
                lblOld, txtOld, lblNew, txtNew, lblConfirm, txtConfirm, lblError, btnOk, btnCancel
            });
        }

        private async void btnOk_Click(object? sender, EventArgs e)
        {
            lblError.Text = "";

            string oldPass = txtOld.Text;
            string newPass = txtNew.Text;
            string confirm = txtConfirm.Text;

            if (string.IsNullOrWhiteSpace(oldPass) || string.IsNullOrWhiteSpace(newPass))
            {
                lblError.Text = "Vui lòng nhập đầy đủ thông tin.";
                return;
            }

            if (newPass.Length < 6)
            {
                lblError.Text = "Mật khẩu mới phải từ 6 ký tự trở lên.";
                return;
            }

            if (newPass != confirm)
            {
                lblError.Text = "Xác nhận mật khẩu không khớp.";
                return;
            }

            try
            {
                btnOk.Enabled = false;

                ProtocolPacket response = await _networkService.RequestAsync(new ProtocolPacket
                {
                    Command = PacketCommand.CHANGE_PASSWORD,
                    Username = _username,
                    PasswordHash = HashHelper.CalculateSha256(Encoding.UTF8.GetBytes(oldPass)),
                    NewPasswordHash = HashHelper.CalculateSha256(Encoding.UTF8.GetBytes(newPass))
                });

                if (response.Command == PacketCommand.AUTH_RESP && response.Success)
                {
                    MessageBox.Show(response.Message ?? "Đổi mật khẩu thành công.",
                        "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    lblError.Text = response.Message ?? "Đổi mật khẩu thất bại.";
                }
            }
            catch (SocketException)
            {
                lblError.Text = "Mất kết nối máy chủ.";
            }
            catch (Exception ex)
            {
                lblError.Text = $"Lỗi: {ex.Message}";
            }
            finally
            {
                btnOk.Enabled = true;
            }
        }
    }
}
