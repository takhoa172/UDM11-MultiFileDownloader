using System;
using System.IO;
using System.Net.Sockets;
using System.Windows.Forms;

namespace Client
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                ApplicationConfiguration.Initialize();

                using var networkService = new NetworkService();

                using (var connForm = new ConnectionForm())
                {
                    if (connForm.ShowDialog() != DialogResult.OK)
                        return;

                    try
                    {
                        networkService.ConnectAsync(connForm.ServerIp, connForm.ServerPort)
                            .GetAwaiter().GetResult();
                    }
                    catch (SocketException ex)
                    {
                        MessageBox.Show($"Không kết nối được Server: {ex.Message}",
                            "Lỗi Kết Nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        MessageBox.Show("Hết thời gian kết nối.",
                            "Timeout", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string loggedUsername = "";

                    using (var loginForm = new LoginForm(networkService))
                    {
                        if (loginForm.ShowDialog() != DialogResult.OK)
                        {
                            networkService.Dispose();
                            return;
                        }
                        loggedUsername = loginForm.LoggedInUsername;
                    }

                    Application.Run(new MainForm(networkService, connForm.ServerIp, connForm.ServerPort, loggedUsername));
                }
            }
            catch (Exception ex)
            {
                try { File.WriteAllText("startup-error.log", ex.ToString()); } catch { }
                MessageBox.Show(ex.ToString(), "Lỗi khởi động");
            }
        }

        public static bool IsValidIPv4Strict(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return false;

            string[] parts = ip.Trim().Split('.');
            if (parts.Length != 4)
                return false;

            foreach (string part in parts)
            {
                if (part.Length == 0 || part.Length > 3)
                    return false;
                if (!int.TryParse(part, out int value))
                    return false;
                if (value < 0 || value > 255)
                    return false;
            }

            return true;
        }
    }
}
