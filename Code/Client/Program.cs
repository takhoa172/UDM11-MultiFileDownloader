using System;
using System.IO;
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
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                try { File.WriteAllText("startup-error.log", ex.ToString()); } catch { }
                MessageBox.Show(ex.ToString(), "Lỗi khởi động");
            }
        }

        // Kiểm tra địa chỉ IPv4 hợp lệ (4 nhóm, mỗi nhóm 0–255)
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