using System;
using System.IO;

namespace Server
{
    public static class ServerLogger
    {
        private static readonly object SyncLock = new object();

        private static readonly string LogDirectory =
            Path.Combine(AppContext.BaseDirectory, "logs");

        private static readonly string LogFilePath =
            Path.Combine(LogDirectory, "server.log");

        public static void Log(string message, string level = "INFO", ConsoleColor color = ConsoleColor.Gray)
        {
            lock (SyncLock)
            {
                try
                {
                    Directory.CreateDirectory(LogDirectory);
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string line = $"[{timestamp}] [{level}] {message}";
                    File.AppendAllText(LogFilePath, line + Environment.NewLine);

                    ConsoleColor old = Console.ForegroundColor;
                    Console.ForegroundColor = color;
                    Console.WriteLine(line);
                    Console.ForegroundColor = old;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[LOG ERROR] Khong ghi duoc log: {e.Message}");
                }
            }
        }

        public static void LogInfo(string message) => Log(message, "INFO", ConsoleColor.Gray);
        public static void LogWarning(string message) => Log(message, "WARN", ConsoleColor.Yellow);
        public static void LogError(string message) => Log(message, "ERROR", ConsoleColor.Red);

        public static void LogConnection(string clientEndpoint) => Log($"Client ket noi: {clientEndpoint}", "CONNECT", ConsoleColor.Green);
        public static void LogDisconnect(string clientEndpoint) => Log($"Client ngat ket noi: {clientEndpoint}", "DISCONNECT", ConsoleColor.DarkYellow);

        public static void LogDownload(string fileName, long bytes) =>
            Log($"Tai file: {fileName} | {bytes} bytes", "DOWNLOAD", ConsoleColor.Yellow);

        public static void LogServerStart(int port) =>
            Log($"Server khoi dong, lang nghe tai cong {port}", "INFO", ConsoleColor.Cyan);

        private static int _onlineClients;
        public static void ClientConnected(string endpoint)
        {
            Interlocked.Increment(ref _onlineClients); LogConnection(endpoint);
            PrintStatus();
        }

        public static void ClientDisconnected(string endpoint)
        {
            Interlocked.Decrement(ref _onlineClients); LogDisconnect(endpoint);
            PrintStatus();
        }

        private static void PrintStatus()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"--- So client dang online: {_onlineClients} ---");
            Console.ResetColor();
        }
    }
}