using System.Collections.Concurrent;
using System.Threading;

namespace RESTar.WebSockets
{
    internal struct CacheItem
    {
        private const int ExpirationMilliseconds = 3000;
        internal byte[] Binary { get; }
        private Timer Timer { get; }
        internal CacheItem(byte[] binary, Timer timer) => (Binary, Timer) = (binary, timer);
        internal void Nudge() => Timer.Change(ExpirationMilliseconds, Timeout.Infinite);
    }

    internal class BinaryCache : ConcurrentDictionary<object, CacheItem>
    {
        internal bool TryGet(object key, out byte[] cached)
        {
            try
            {
                if (TryGetValue(key, out var cacheItem))
                {
                    cacheItem.Nudge();
                    cached = cacheItem.Binary;
                    return true;
                }
                cached = null;
                return false;
            }
            catch
            {
                cached = null;
                return false;
            }
        }

        internal void Cache(object key, byte[] binary)
        {
            try
            {
                var timer = new Timer(state =>
                {
                    TryRemove(key, out _);
                    ((Timer) state).Dispose();
                });
                var cacheItem = new CacheItem(binary, timer);
                cacheItem.Nudge();
                TryAdd(key, cacheItem);
            }
            catch { }
        }
    }
}