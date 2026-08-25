using System;
using System.Windows.Forms;
using Client;

namespace Client
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            Console.Write("Nhap IP Server (vi du 127.0.0.1): ");
            string serverIp = ReadRequiredInput();

            Console.Write("Nhap Port Server (vi du 8080): ");
            int serverPort = ReadPort();

            Application.Run(new MainForm(serverIp, serverPort));
        }

        static string ReadRequiredInput()
        {
            while (true)
            {
                string? input = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(input))
                {
                    return input.Trim();
                }

                Console.Write("Gia tri khong duoc rong, nhap lai: ");
            }
        }

        static int ReadPort()
        {
            while (true)
            {
                string input = ReadRequiredInput();
                if (int.TryParse(input, out int port) && port is > 0 and <= 65535)
                {
                    return port;
                }

                Console.Write("Port khong hop le, nhap lai: ");
            }
        }
    }
}