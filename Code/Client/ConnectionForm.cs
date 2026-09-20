using System;
using System.Windows.Forms;

namespace Client
{
    public partial class ConnectionForm : Form
    {
        public string ServerIp => txtIp.Text.Trim();
        public int ServerPort { get; private set; }

        public ConnectionForm()
        {
            InitializeComponent();
            txtIp.Text = ClientConfig.Settings.Network.ServerIp;
            txtPort.Text = ClientConfig.Settings.Network.ServerPort.ToString();
            txtIp.Focus();
            txtIp.SelectAll();
        }

        private void btnConnect_Click(object? sender, EventArgs e)
        {
            lblError.Text = "";

            string ip = txtIp.Text.Trim();
            string portText = txtPort.Text.Trim();

            if (!Program.IsValidIPv4Strict(ip))
            {
                lblError.Text = "Địa chỉ IP không hợp lệ!";
                txtIp.Focus();
                txtIp.SelectAll();
                return;
            }

            if (ip == "0.0.0.0" || ip == "255.255.255.255")
            {
                lblError.Text = "Địa chỉ IP không hợp lệ!";
                txtIp.Focus();
                txtIp.SelectAll();
                return;
            }

            if (!int.TryParse(portText, out int port) || port <= 0 || port > 65535)
            {
                lblError.Text = "Cổng phải từ 1 đến 65535!";
                txtPort.Focus();
                txtPort.SelectAll();
                return;
            }

            ServerPort = port;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
