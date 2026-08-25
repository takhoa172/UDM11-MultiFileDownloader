using System;
using System.Threading.Tasks;

namespace Server
{
    public sealed class RateLimiter
    {
        private readonly double _bytesPerSecond;
        private readonly object _sync = new object();
        private double _tokens;
        private long _lastTicks;
        public const long Unlimited = -1;

        public RateLimiter(long bytesPerSecond)
        {
            if (bytesPerSecond != Unlimited && bytesPerSecond <= 0)
                throw new ArgumentOutOfRangeException(nameof(bytesPerSecond));
            _bytesPerSecond = bytesPerSecond;
            _tokens = 0;
            _lastTicks = DateTime.UtcNow.Ticks;
        }

        public long BytesPerSecond => (long)_bytesPerSecond;

        public async Task ThrottleAsync(int bytes)
        {
            if (_bytesPerSecond == Unlimited || bytes <= 0)
                return;
            while (true)
            {
                double waitMs;
                lock (_sync)
                {
                    long nowTicks = DateTime.UtcNow.Ticks;
                    double elapsedSeconds = (nowTicks - _lastTicks) / (double)TimeSpan.TicksPerSecond;
                    _tokens = Math.Min(_bytesPerSecond, _tokens + elapsedSeconds * _bytesPerSecond);
                    _lastTicks = nowTicks;

                    if (bytes <= _tokens)
                    {
                        _tokens -= bytes;
                        return;
                    }

                    double missing = bytes - _tokens;
                    waitMs = missing / _bytesPerSecond * 1000.0;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(Math.Max(1, waitMs)));
            }
        }
    }

}
