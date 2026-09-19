using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Shared;

namespace Server
{
    public sealed class UserInfo
    {
        public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public long SpeedLimitMBs { get; set; } = 5;
    }

    public sealed class UserStore
    {
        private static readonly string UsersFilePath =
            Path.Combine(AppContext.BaseDirectory, "users.json");

        private static readonly object _lock = new();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        private static List<UserInfo> LoadUsers()
        {
            if (!File.Exists(UsersFilePath))
                return new List<UserInfo>();

            string json = File.ReadAllText(UsersFilePath);
            return JsonSerializer.Deserialize<List<UserInfo>>(json, JsonOptions)
                   ?? new List<UserInfo>();
        }

        public static List<UserInfo> LoadUsersForCheck()
        {
            lock (_lock)
            {
                return LoadUsers();
            }
        }

        private static void SaveUsers(List<UserInfo> users)
        {
            string json = JsonSerializer.Serialize(users, JsonOptions);
            File.WriteAllText(UsersFilePath, json);
        }

        private static string HashPassword(string password)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            return HashHelper.CalculateSha256(bytes);
        }

        public static (bool Success, string Message) Register(
            string username, string password)
        {
            lock (_lock)
            {
                try
                {
                    var users = LoadUsers();

                    if (users.Any(u =>
                        u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                    {
                        return (false, "Tên tài khoản đã tồn tại.");
                    }

                    users.Add(new UserInfo
                    {
                        Username = username,
                        PasswordHash = HashPassword(password)
                    });

                    SaveUsers(users);
                    return (true, "Đăng ký thành công.");
                }
                catch (Exception ex)
                {
                    return (false, $"Lỗi server: {ex.Message}");
                }
            }
        }

        public static (bool Success, string Message) Login(
            string username, string password)
        {
            lock (_lock)
            {
                var users = LoadUsers();

                var user = users.FirstOrDefault(u =>
                    u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                if (user == null)
                    return (false, "Tên tài khoản không tồn tại.");

                if (user.PasswordHash != HashPassword(password))
                    return (false, "Mật khẩu không đúng.");

                return (true, "Đăng nhập thành công.");
            }
        }

        public static (bool Success, string Message) ResetPassword(
            string username, string newPassword)
        {
            lock (_lock)
            {
                try
                {
                    var users = LoadUsers();

                    var user = users.FirstOrDefault(u =>
                        u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                    if (user == null)
                        return (false, "Tên tài khoản không tồn tại.");

                    user.PasswordHash = HashPassword(newPassword);
                    SaveUsers(users);

                    return (true, "Đặt lại mật khẩu thành công.");
                }
                catch (Exception ex)
                {
                    return (false, $"Lỗi server: {ex.Message}");
                }
            }
        }

        public static long GetSpeedLimit(string username)
        {
            lock (_lock)
            {
                var users = LoadUsers();
                var user = users.FirstOrDefault(u =>
                    u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
                return user?.SpeedLimitMBs ?? 5;
            }
        }

        public static (bool Success, string Message) SetSpeedLimit(
            string username, long speedMBs)
        {
            lock (_lock)
            {
                try
                {
                    var users = LoadUsers();
                    var user = users.FirstOrDefault(u =>
                        u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                    if (user == null)
                        return (false, "Tên tài khoản không tồn tại.");

                    user.SpeedLimitMBs = speedMBs;
                    SaveUsers(users);

                    return (true, $"Đã đặt tốc độ tải {speedMBs} MB/s.");
                }
                catch (Exception ex)
                {
                    return (false, $"Lỗi server: {ex.Message}");
                }
            }
        }
    }
}
