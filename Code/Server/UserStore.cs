using System.Text.Json;

namespace Server;

public sealed class UserAccount
{
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
}

public static class UserStore
{
    private static readonly string FilePath =
        Path.Combine(AppContext.BaseDirectory, "users.json");

    private static readonly object LockObject = new();

    private static List<UserAccount> LoadUsers()
    {
        lock (LockObject)
        {
            if (!File.Exists(FilePath))
                return new List<UserAccount>();

            string json = File.ReadAllText(FilePath);

            if (string.IsNullOrWhiteSpace(json))
                return new List<UserAccount>();

            return JsonSerializer.Deserialize<List<UserAccount>>(json)
                   ?? new List<UserAccount>();
        }
    }

    private static void SaveUsers(List<UserAccount> users)
    {
        lock (LockObject)
        {
            string json = JsonSerializer.Serialize(
                users,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(FilePath, json);
        }
    }

    public static bool Register(string username, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        List<UserAccount> users = LoadUsers();

        if (users.Any(x =>
            string.Equals(x.Username, username,
                StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        users.Add(new UserAccount
        {
            Username = username.Trim(),
            PasswordHash = passwordHash
        });

        SaveUsers(users);

        return true;
    }

    public static bool ValidateLogin(
        string username,
        string passwordHash)
    {
        List<UserAccount> users = LoadUsers();

        UserAccount? user = users.FirstOrDefault(x =>
            string.Equals(
                x.Username,
                username,
                StringComparison.OrdinalIgnoreCase));

        if (user == null)
            return false;

        return user.PasswordHash == passwordHash;
    }

    public static bool ChangePassword(
        string username,
        string newPasswordHash)
    {
        List<UserAccount> users = LoadUsers();

        UserAccount? user = users.FirstOrDefault(x =>
            string.Equals(
                x.Username,
                username,
                StringComparison.OrdinalIgnoreCase));

        if (user == null)
            return false;

        user.PasswordHash = newPasswordHash;

        SaveUsers(users);

        return true;
    }
}