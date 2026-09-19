using System;
using System.Threading.Tasks;

namespace Server
{
    public sealed class RateLimiter
    {
        private double _bytesPerSecond;
        private double _maxTokens;
        private readonly object _sync = new object();
        private double _tokens;
        private long _lastTicks;
        public const long Unlimited = -1;

        public RateLimiter(long bytesPerSecond, long maxBurstBytes = 0)
        {
            if (bytesPerSecond != Unlimited && bytesPerSecond <= 0)
                throw new ArgumentOutOfRangeException(nameof(bytesPerSecond));
            _bytesPerSecond = bytesPerSecond;
            _maxTokens = maxBurstBytes > 0 ? maxBurstBytes : bytesPerSecond;
            _tokens = 0;
            _lastTicks = DateTime.UtcNow.Ticks;
        }

        public long BytesPerSecond => (long)_bytesPerSecond;

        public void SetRate(long bytesPerSecond)
        {
            lock (_sync)
            {
                _bytesPerSecond = bytesPerSecond;
                _maxTokens = bytesPerSecond;
                _tokens = Math.Min(_tokens, _maxTokens);
                _lastTicks = DateTime.UtcNow.Ticks;
            }
        }

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
                    _tokens = Math.Min(_maxTokens, _tokens + elapsedSeconds * _bytesPerSecond);
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
