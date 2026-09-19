using System.Net.Sockets;

namespace Server;

public static class SocketTimeoutManager
{
    public static void Apply(TcpClient client, int readMs, int writeMs)
    {
        client.ReceiveTimeout = readMs;
        client.SendTimeout = writeMs;
    }

    public static CancellationTokenSource CreateLinkedCts(
        int timeoutMs,
        CancellationToken outer = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(outer);
        cts.CancelAfter(timeoutMs);
        return cts;
    }
}
