using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using Shared;

namespace Client
{
    public partial class MainForm : Form
    {
        private bool _isConnected = false;
        private readonly NetworkService _networkService = new NetworkService();

        private readonly BindingList<FileItem> _serverFiles = new BindingList<FileItem>();
        private readonly BindingList<FileItem> _downloadFiles = new BindingList<FileItem>();

        private readonly string _serverIp;
        private readonly int _serverPort;

        public MainForm(string ip, int port)
        {
            InitializeComponent();

            _serverIp = string.IsNullOrWhiteSpace(ip) ? "127.0.0.1" : ip;
            _serverPort = port > 0 ? port : 8080;

            SetupGridViews();

            if (Controls.Find("lblServerIp", true).Length > 0)
            {
                lblIP.Text = $"IP Server: {_serverIp}";
            }

            if (Controls.Find("lblServerPort", true).Length > 0)
            {
                lblPort.Text = $"Port: {_serverPort}";
            }

            _ = ConnectToServerAsync();
        }

        private void SetupGridViews()
        {
            dgvServer.DataSource = _serverFiles;
            dgvDownload.DataSource = _downloadFiles;

            if (dgvServer.Columns["FileName"] != null)
                dgvServer.Columns["FileName"].HeaderText = "Tên File";
            if (dgvServer.Columns["FormattedSize"] != null)
                dgvServer.Columns["FormattedSize"].HeaderText = "Kích Thước";

            if (dgvDownload.Columns["FileName"] != null)
                dgvDownload.Columns["FileName"].HeaderText = "Tên File";
            if (dgvDownload.Columns["FormattedSize"] != null)
                dgvDownload.Columns["FormattedSize"].HeaderText = "Kích Thước";

            if (dgvServer.Columns.Contains("FileSizeBytes"))
                dgvServer.Columns["FileSizeBytes"].Visible = false;
            if (dgvDownload.Columns.Contains("FileSizeBytes"))
                dgvDownload.Columns["FileSizeBytes"].Visible = false;

            dgvDownload.AllowDrop = true;
        }

        public void SetConnectionState(bool isConnected, string customStatus = "")
        {
            _isConnected = isConnected;

            if (isConnected)
            {
                lblStatus.Text = string.IsNullOrEmpty(customStatus) ? "● Đã kết nối" : customStatus;
                lblStatus.ForeColor = Color.ForestGreen;
            }
            else
            {
                lblStatus.Text = string.IsNullOrEmpty(customStatus) ? "● Chưa kết nối" : customStatus;
                lblStatus.ForeColor = Color.Red;

                _serverFiles.Clear();
                _downloadFiles.Clear();
            }
        }

        private async Task ConnectToServerAsync()
        {
            try
            {
                lblStatus.Text = "● Đang kết nối...";
                lblStatus.ForeColor = Color.Orange;

                await _networkService.ConnectAsync(_serverIp, _serverPort);

                SetConnectionState(true);

                await FetchServerFileListAsync();
            }
            catch (Exception ex)
            {
                SetConnectionState(false, "● Kết nối thất bại");
                MessageBox.Show($"Lỗi kết nối Server ({_serverIp}:{_serverPort}): {ex.Message}", "Lỗi Kết Nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task FetchServerFileListAsync()
        {
            try
            {
                await _networkService.SendPacketAsync(new ProtocolPacket
                {
                    Command = PacketCommand.GET_LIST
                });

                ProtocolPacket response = await _networkService.ReadPacketAsync();

                if (response.Command == PacketCommand.ERROR_RESP)
                {
                    MessageBox.Show($"ERROR: {response.ErrorCode} - {response.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string rawData = PacketHelper.DecodeTextData(response.DataBase64);

                _serverFiles.Clear();
                if (string.IsNullOrWhiteSpace(rawData)) return;

                string[] lines = rawData.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    string fileName = line;
                    long fileSize = 0;

                    if (line.Contains("|"))
                    {
                        string[] parts = line.Split('|');
                        fileName = parts[0];
                        long.TryParse(parts[1], out fileSize);
                    }

                    _serverFiles.Add(new FileItem
                    {
                        FileName = fileName.Trim(),
                        FileSizeBytes = fileSize
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERROR: {ex.Message}", "Lỗi Dữ Liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _networkService?.Dispose();
            base.OnFormClosing(e);
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e) { }
        private void lblStatus_Click(object sender, EventArgs e) { }
        private void label1_Click(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }
    }
}