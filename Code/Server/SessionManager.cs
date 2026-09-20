namespace Server;

public static class SessionManager
{
    private static readonly object SyncLock = new();
    private static readonly Dictionary<string, ActiveSession> SessionsByUsername =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, ActiveSession> SessionsByConnection =
        new(StringComparer.Ordinal);

    public static bool TryStart(string username, string connectionId, out string token)
    {
        lock (SyncLock)
        {
            if (SessionsByUsername.ContainsKey(username) ||
                SessionsByConnection.ContainsKey(connectionId))
            {
                token = string.Empty;
                return false;
            }

            ActiveSession session = new(username, connectionId, Guid.NewGuid().ToString("N"));
            SessionsByUsername.Add(username, session);
            SessionsByConnection.Add(connectionId, session);
            token = session.Token;
            return true;
        }
    }

    public static void End(string connectionId)
    {
        lock (SyncLock)
        {
            if (!SessionsByConnection.Remove(connectionId, out ActiveSession? session))
                return;

            SessionsByUsername.Remove(session.Username);
        }
    }

    public static bool TryValidateToken(
        string? username,
        string? token,
        out string authenticatedUsername)
    {
        authenticatedUsername = string.Empty;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(token))
            return false;

        lock (SyncLock)
        {
            if (!SessionsByUsername.TryGetValue(username, out ActiveSession? session) ||
                !string.Equals(session.Token, token, StringComparison.Ordinal))
            {
                return false;
            }

            authenticatedUsername = session.Username;
            return true;
        }
    }

    private sealed record ActiveSession(string Username, string ConnectionId, string Token);
}
