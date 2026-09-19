using System;
using System.Windows.Forms;
using Client;

namespace Client.Pages
{
    public class SettingPage : UserControl
    {
        private ClientSettings _currentSettings = new ClientSettings();

        // Khai báo giao diện 
        private ComboBox cbMaxDownloads = new ComboBox();
        private ComboBox cbSpeedLimit = new ComboBox();
        private Button btnSave = new Button();

        public SettingPage()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            Label lblMax = new Label() { Text = "Số file tải cùng lúc:", Top = 20, Left = 20, Width = 150 };
            cbMaxDownloads.Items.AddRange(new object[] { "1", "2", "3", "4", "5" });
            cbMaxDownloads.Top = 20; cbMaxDownloads.Left = 180; cbMaxDownloads.DropDownStyle = ComboBoxStyle.DropDownList;

            Label lblSpeed = new Label() { Text = "Giới hạn tốc độ (MB/s):", Top = 60, Left = 20, Width = 150 };
            cbSpeedLimit.Items.AddRange(new object[] { "1", "5", "10" });
            cbSpeedLimit.Top = 60; cbSpeedLimit.Left = 180; cbSpeedLimit.DropDownStyle = ComboBoxStyle.DropDownList;

            btnSave.Text = "Lưu Cấu Hình";
            btnSave.Top = 100; btnSave.Left = 180; btnSave.Width = 100;
            btnSave.Click += btnSave_Click;

            this.Controls.Add(lblMax);
            this.Controls.Add(cbMaxDownloads);
            this.Controls.Add(lblSpeed);
            this.Controls.Add(cbSpeedLimit);
            this.Controls.Add(btnSave);

            this.ResumeLayout(false);
        }

        private void LoadCurrentSettings()
        {
            _currentSettings = ClientSettings.Load();
            cbMaxDownloads.SelectedItem = _currentSettings.Download.MaxConcurrentDownloads.ToString();
            cbSpeedLimit.SelectedItem = _currentSettings.Network.RequestedRateMBps.ToString();
        }

        private void btnSave_Click(object? sender, EventArgs e)
        {
            if (cbMaxDownloads.SelectedItem != null && cbSpeedLimit.SelectedItem != null)
            {
                _currentSettings.Download.MaxConcurrentDownloads = int.Parse(cbMaxDownloads.SelectedItem.ToString() ?? "3");
                _currentSettings.Network.RequestedRateMBps = int.Parse(cbSpeedLimit.SelectedItem.ToString() ?? "5");

                _currentSettings.Save();
                MessageBox.Show("Đã lưu cấu hình thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}