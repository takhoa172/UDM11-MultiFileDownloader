using System;
using System.Collections.Generic;
using System.Text;

namespace Server;

public static class ServerConfig
{
    public static readonly RateLimiter DownloadLimiter = new RateLimiter(5 * 1024 * 1024);
}