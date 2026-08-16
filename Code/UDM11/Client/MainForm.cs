using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Client
{
    public partial class MainForm : Form
{
    // Biến lưu trạng thái kết nối
    private bool _isConnected = false;

    // Delegate sự kiện linh hoạt để ghép với thành viên khác sau này
    public event Action<string, int>? OnConnectRequested;
    public event Action? OnDisconnectRequested;

    public MainForm()
    {
        InitializeComponent();

        // Khởi tạo trạng thái giao diện mặc định
        SetConnectionState(false);
    }

    /// <summary>
    /// Hàm dùng chung để cập nhật toàn bộ trạng thái kết nối trên UI
    /// </summary>
    public void SetConnectionState(bool isConnected, string customStatus = "")
    {
        _isConnected = isConnected;

        if (isConnected)
        {
            lblStatus.Text = string.IsNullOrEmpty(customStatus) ? "● Đã kết nối" : customStatus;
            lblStatus.ForeColor = Color.ForestGreen;

            btnConnect.Text = "Ngắt kết nối";

            // Khóa ô nhập liệu khi đã kết nối
            txtServerIp.Enabled = false;
            txtServerPort.Enabled = false;
        }
        else
        {
            lblStatus.Text = string.IsNullOrEmpty(customStatus) ? "● Chưa kết nối" : customStatus;
            lblStatus.ForeColor = Color.Red;

            btnConnect.Text = "Kết nối";

            // Mở lại ô nhập liệu khi chưa kết nối
            txtServerIp.Enabled = true;
            txtServerPort.Enabled = true;
        }
    }

        /// <summary>
        /// Xử lý sự kiện khi bấm nút Kết nối / Ngắt kết nối
        /// </summary>
        private void btnConnect_Click(object sender, EventArgs e)
        {
            // 1. Nếu đang ĐÃ KẾT NỐI -> Bấm vào để Ngắt kết nối
            if (_isConnected)
            {
                if (OnDisconnectRequested != null)
                {
                    OnDisconnectRequested.Invoke();
                }
                else
                {
                    SetConnectionState(false); // Chạy độc lập test UI
                }
                return;
            }

            // 2. Validate dữ liệu đầu vào
            string ip = txtServerIp.Text.Trim();
            string portText = txtServerPort.Text.Trim();

            if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(portText))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ IP Server và Port!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(portText, out int port) || port <= 0 || port > 65535)
            {
                MessageBox.Show("Port phải là số nguyên hợp lệ (1 - 65535)!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 3. Thực hiện chuyển trạng thái
            if (OnConnectRequested != null)
            {
                lblStatus.Text = "● Đang kết nối...";
                lblStatus.ForeColor = Color.Orange;
                OnConnectRequested.Invoke(ip, port);
            }
            else
            {
                // Chạy độc lập test UI cho Task 12
                SetConnectionState(true);
            }
            //    lblStatus.Text = "● Đang kết nối...";
            //    lblStatus.ForeColor = Color.Orange;
            //    btnConnect.Enabled = false; // Khóa nút tạm thời để tránh spam click

            //    // ⏳ 2. Giả lập chờ Server phản hồi trong 1.5 giây (Không gây treo UI)
            //    await Task.Delay(1500);

            //    // 🟢 3. Chuyển sang ĐÃ KẾT NỐI
            //    btnConnect.Enabled = true;
            //    SetConnectionState(true);
        }
    }
}
