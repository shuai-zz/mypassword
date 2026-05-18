using System;
using System.Threading;

namespace MyPasswordDesktop.Util
{
    /// <summary>
    /// Generates monotonically increasing millisecond-based ids — the C#
    /// equivalent of the Java <c>IdUtils</c> AtomicLong loop.
    /// </summary>
    public static class IdUtils
    {
        private static long _prevId;

        public static long NextId()
        {
            for (; ; )
            {
                long currentMax = Interlocked.Read(ref _prevId);
                long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long next = (ts <= currentMax) ? currentMax + 1 : ts;
                if (Interlocked.CompareExchange(ref _prevId, next, currentMax) == currentMax)
                {
                    return next;
                }
            }
        }
    }
}
