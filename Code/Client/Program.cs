namespace Client
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Cấu hình khởi tạo các thiết lập mặc định của WinForms (.NET 6+)
            ApplicationConfiguration.Initialize();

            // Khởi chạy ứng dụng với giao diện MainForm chính
            Application.Run(new MainForm());
        }
    }
}