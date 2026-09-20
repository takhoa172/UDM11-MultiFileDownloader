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

                string serverIp = ClientConfig.Settings.Network.ServerIp;
                int serverPort = ClientConfig.Settings.Network.ServerPort;
                bool showConnectionDialog = true;

                while (true)
                {
                    if (showConnectionDialog)
                    {
                        using var connForm = new ConnectionForm();
                        if (connForm.ShowDialog() != DialogResult.OK)
                            return;

                        serverIp = connForm.ServerIp;
                        serverPort = connForm.ServerPort;
                    }

                    using var networkService = new NetworkService();

                    try
                    {
                        networkService.ConnectAsync(serverIp, serverPort)
                            .GetAwaiter().GetResult();
                    }
                    catch (SocketException ex)
                    {
                        if (!showConnectionDialog)
                        {
                            showConnectionDialog = true;
                            continue;
                        }

                        MessageBox.Show($"Không kết nối được máy chủ: {ex.Message}",
                            "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        if (!showConnectionDialog)
                        {
                            showConnectionDialog = true;
                            continue;
                        }

                        MessageBox.Show("Hết thời gian kết nối.",
                            "Hết thời gian", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string loggedUsername = "";
                    string loggedToken = "";

                    using (var loginForm = new LoginForm(networkService))
                    {
                        if (loginForm.ShowDialog() != DialogResult.OK)
                        {
                            networkService.Dispose();
                            return;
                        }
                        loggedUsername = loginForm.LoggedInUsername;
                        loggedToken = loginForm.LoggedInToken;
                    }

                    using var mainForm = new MainForm(
                        networkService,
                        serverIp,
                        serverPort,
                        loggedUsername,
                        loggedToken);

                    Application.Run(mainForm);

                    if (!mainForm.LogoutRequested)
                        return;

                    // Logout requested: reconnect to the last endpoint and show Login again.
                    serverIp = ClientConfig.Settings.Network.ServerIp;
                    serverPort = ClientConfig.Settings.Network.ServerPort;
                    showConnectionDialog = false;
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
