using System.Text.Json;
using Shared;

namespace Server;

public sealed class UserAccount
{
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Salt { get; set; } = "";
}

public static class UserStore
{
    private static readonly object SyncLock = new();
    private static string FilePath => Path.Combine(ServerConfig.ProjectRoot, "users.json");

    public static bool Register(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return false;

        List<UserAccount> users = LoadUsers();

        if (users.Any(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase)))
            return false;

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

    public static bool ValidateLogin(string username, string password)
    {
        UserAccount? user = LoadUsers().FirstOrDefault(u =>
            string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));

        if (user == null || string.IsNullOrEmpty(user.Salt))
            return false;

        return PasswordHasher.Verify(password, user.Salt, user.PasswordHash);
    }

    public static bool ChangePassword(string username, string newPassword)
    {
        List<UserAccount> users = LoadUsers();

        UserAccount? user = users.FirstOrDefault(u =>
            string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));

        if (user == null)
            return false;

        user.Salt = PasswordHasher.GenerateSalt();
        user.PasswordHash = PasswordHasher.HashPassword(newPassword, user.Salt);

        SaveUsers(users);
        return true;
    }

    private static List<UserAccount> LoadUsers()
    {
        lock (SyncLock)
        {
            if (!File.Exists(FilePath))
                return new List<UserAccount>();

            string json = File.ReadAllText(FilePath);

            if (string.IsNullOrWhiteSpace(json))
                return new List<UserAccount>();

            return JsonSerializer.Deserialize<List<UserAccount>>(json) ?? new List<UserAccount>();
        }
    }

    private static void SaveUsers(List<UserAccount> users)
    {
        lock (SyncLock)
        {
            string json = JsonSerializer.Serialize(users,
                new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(FilePath, json);
        }
    }
}
