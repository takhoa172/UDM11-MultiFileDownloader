namespace Server;

public sealed class ClientSessionContext
{
    public ClientSessionContext(string connectionId)
    {
        ConnectionId = connectionId;
    }

    public string ConnectionId { get; }
    public string? Username { get; set; }
    public string? Token { get; set; }
}
