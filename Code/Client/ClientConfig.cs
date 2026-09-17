namespace Client;

public static class ClientConfig
{
    public static ClientSettings Settings { get; } = ClientSettings.Load();
}
