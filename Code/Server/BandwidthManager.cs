namespace Server;

public sealed class BandwidthSession
{
    public long RequestedRate { get; init; }
    public RateLimiter Limiter { get; } = new RateLimiter(ServerConfig.TotalBytesPerSecond);
}

public static class BandwidthManager
{
    private static readonly object SyncLock = new();
    private static readonly List<BandwidthSession> Sessions = new();

    public static BandwidthSession BeginSession(long requestedRate)
    {
        var session = new BandwidthSession { RequestedRate = requestedRate };

        lock (SyncLock)
        {
            Sessions.Add(session);
            Recalculate();
        }

        return session;
    }

    public static void EndSession(BandwidthSession session)
    {
        lock (SyncLock)
        {
            Sessions.Remove(session);
            Recalculate();
        }
    }

    private static void Recalculate()
    {
        int count = Math.Max(1, Sessions.Count);
        long fairShare = ServerConfig.TotalBytesPerSecond / count;

        foreach (BandwidthSession session in Sessions)
        {
            long rate = session.RequestedRate > 0
                ? Math.Min(session.RequestedRate, fairShare)
                : fairShare;

            session.Limiter.SetRate(rate);
        }
    }
}
