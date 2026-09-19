using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server
{
    public static class ServerLogger
    {
        private static readonly object SyncLock = new object();

        private static readonly string LogDirectory = ServerConfig.LogDirectory;

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
                    Console.WriteLine($"[LOG ERROR] Không ghi được log: {e.Message}");
                }
            }
        }

        public static void LogInfo(string message) => Log(message, "INFO", ConsoleColor.Gray);
        public static void LogWarning(string message) => Log(message, "WARN", ConsoleColor.Yellow);
        public static void LogError(string message) => Log(message, "ERROR", ConsoleColor.Red);

        public static void LogDownload(string fileName, long bytes) =>
            Log($"Tải file: {fileName} | {bytes} bytes", "DOWNLOAD", ConsoleColor.Yellow);

        public static void LogServerStart(int port) =>
            Log($"Server khởi động, lắng nghe tại cổng {port}", "INFO", ConsoleColor.Cyan);

        //  ĐẾM CLIENT ONLINE

        private static readonly Dictionary<string, int> _connectionCountByIp = new();
        private static readonly object _ipLock = new object();
        private static int _lastPrintedCount = -1;

        public static void ClientConnected(string endpoint, bool silent = false)
        {
            string ip = GetIpFromEndpoint(endpoint);

            lock (_ipLock)
            {
                if (!_connectionCountByIp.ContainsKey(ip))
                    _connectionCountByIp[ip] = 0;

                _connectionCountByIp[ip]++;
            }

            if (!silent)
            {
                Log($"Client kết nối: {endpoint}", "CONNECT", ConsoleColor.Green);
            }

            PrintStatusIfChanged();
        }

        public static void ClientDisconnected(string endpoint, bool silent = false)
        {
            string ip = GetIpFromEndpoint(endpoint);

            lock (_ipLock)
            {
                if (_connectionCountByIp.ContainsKey(ip))
                {
                    _connectionCountByIp[ip]--;

                    if (_connectionCountByIp[ip] <= 0)
                        _connectionCountByIp.Remove(ip);
                }
            }

            if (!silent)
            {
                Log($"Client ngắt kết nối: {endpoint}", "DISCONNECT", ConsoleColor.DarkYellow);
            }

            PrintStatusIfChanged();
        }

        private static string GetIpFromEndpoint(string endpoint)
        {
            int colonIndex = endpoint.LastIndexOf(':');
            if (colonIndex > 0)
            {
                string ip = endpoint.Substring(0, colonIndex);

                if (ip == "[::1]" || ip == "::1")
                    return "127.0.0.1";

                return ip;
            }
            return endpoint;
        }

        private static void PrintStatusIfChanged()
        {
            int currentCount;
            lock (_ipLock)
            {
                currentCount = _connectionCountByIp.Values.Sum();
            }

            if (currentCount == _lastPrintedCount)
                return;

            _lastPrintedCount = currentCount;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"--- Số client đang online: {currentCount} ---");
            Console.ResetColor();
        }
    }
}
