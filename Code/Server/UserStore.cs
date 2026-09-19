using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Shared;

namespace Server
{
    public sealed class UserAccount
    {
        public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string Salt { get; set; } = "";
        public long SpeedLimitMBs { get; set; } = 5;
    }

    public static class UserStore
    {
        private static readonly object _lock = new();

        private static string FilePath =>
            Path.Combine(AppContext.BaseDirectory, "users.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public static bool Register(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            lock (_lock)
            {
                try
                {
                    var users = LoadUsers();

                    if (users.Any(u =>
                        u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                    {
                        return false;
                    }

                    string salt = PasswordHasher.GenerateSalt();

                    users.Add(new UserAccount
                    {
                        Username = username.Trim(),
                        Salt = salt,
                        PasswordHash = PasswordHasher.HashPassword(password, salt)
                    });

                    SaveUsers(users);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public static bool ValidateLogin(string username, string password)
        {
            lock (_lock)
            {
                var users = LoadUsers();

                var user = users.FirstOrDefault(u =>
                    u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                if (user == null || string.IsNullOrEmpty(user.Salt))
                    return false;

                return PasswordHasher.Verify(password, user.Salt, user.PasswordHash);
            }
        }

        public static bool ChangePassword(string username, string newPassword)
        {
            lock (_lock)
            {
                try
                {
                    var users = LoadUsers();

                    var user = users.FirstOrDefault(u =>
                        u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                    if (user == null)
                        return false;

                    user.Salt = PasswordHasher.GenerateSalt();
                    user.PasswordHash = PasswordHasher.HashPassword(newPassword, user.Salt);

                    SaveUsers(users);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public static List<UserAccount> LoadUsersForCheck()
        {
            lock (_lock)
            {
                return LoadUsers();
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

        private static List<UserAccount> LoadUsers()
        {
            if (!File.Exists(FilePath))
                return new List<UserAccount>();

            string json = File.ReadAllText(FilePath);

            if (string.IsNullOrWhiteSpace(json))
                return new List<UserAccount>();

            return JsonSerializer.Deserialize<List<UserAccount>>(json, JsonOptions)
                   ?? new List<UserAccount>();
        }

        private static void SaveUsers(List<UserAccount> users)
        {
            string json = JsonSerializer.Serialize(users, JsonOptions);
            File.WriteAllText(FilePath, json);
        }
    }
}
