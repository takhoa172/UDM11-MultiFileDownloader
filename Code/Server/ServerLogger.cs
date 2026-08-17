using System;
using System.IO;
using Microsoft.VisualBasic;

namespace Server
{
    public static class ServerLogger
    {
        private static readonly object SyncLock = new object();

        private static readonly string LogDirectory =
            Path.Combine(AppContext.BaseDirectory, "logs");

        private static readonly string LogFilePath =
            Path.Combine(LogDirectory, "server.log");

        public static void Log(string message, string level = "INFO")
        {
            try
            {
                lock (SyncLock)
                {
                    Directory.CreateDirectory(LogDirectory);
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string line = $"[{timestamp}] [{level}] {message}";
                    File.AppendAllText(LogFilePath, line + Environment.NewLine);
                    Console.WriteLine(line);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[LOG ERROR] Khong ghi duoc log: {e.Message}");
            }
        }

        public static void LogInfo(string message) => Log(message, "INFO");
        public static void LogWarning(string message) => Log(message, "WARN");
        public static void LogError(string message) => Log(message, "ERROR");

        public static void LogConnection(string clientEndpoint) => Log($"Client ket noi: {clientEndpoint}");
        public static void LogDisconnect(string clientEndpoint) => Log($"Client ngat ket noi: {clientEndpoint}");

        public static void LogDownload(string fileName, long bytes) =>
            Log($"Tai file: {fileName} | {bytes} bytes (chi metadata, khong ghi noi dung file)");
    }
}