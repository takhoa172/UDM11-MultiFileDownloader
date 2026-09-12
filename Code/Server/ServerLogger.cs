using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

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
                    Console.WriteLine($"[LOG ERROR] Khong ghi duoc log: {e.Message}");
                }
            }
        }

        public static void LogInfo(string message) => Log(message, "INFO", ConsoleColor.Gray);
        public static void LogWarning(string message) => Log(message, "WARN", ConsoleColor.Yellow);
        public static void LogError(string message) => Log(message, "ERROR", ConsoleColor.Red);

        public static void LogDownload(string fileName, long bytes) =>
            Log($"Tai file: {fileName} | {bytes} bytes", "DOWNLOAD", ConsoleColor.Yellow);

        public static void LogServerStart(int port) =>
            Log($"Server khoi dong, lang nghe tai cong {port}", "INFO", ConsoleColor.Cyan);

        // ─────────────────────────────────────────────────────────
        //  ĐẾM CLIENT ONLINE — LOG CONNECT/DISCONNECT CÓ CHỌN LỌC
        // ─────────────────────────────────────────────────────────

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
                Log($"Client ket noi: {endpoint}", "CONNECT", ConsoleColor.Green);
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
                Log($"Client ngat ket noi: {endpoint}", "DISCONNECT", ConsoleColor.DarkYellow);
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
                currentCount = _connectionCountByIp.Count;
            }

            if (currentCount == _lastPrintedCount)
                return;

            _lastPrintedCount = currentCount;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"--- So client dang online: {currentCount} ---");
            Console.ResetColor();
        }
    }
}