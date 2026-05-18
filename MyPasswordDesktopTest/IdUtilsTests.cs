using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MyPasswordDesktop.Util;

namespace MyPasswordDesktopTest
{
    /// <summary>Ported from <c>desktop/.../util/IdUtilsTest.java</c>.</summary>
    public class IdUtilsTests
    {
        [Test]
        public void TestNextId()
        {
            const int threadCount = 100;
            const int iterations = 1000;
            var ids = new ConcurrentDictionary<long, bool>();

            // a start gate so all threads contend at once, as in the Java test
            using var startGate = new ManualResetEventSlim(false);
            var tasks = new Task[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    startGate.Wait();
                    for (int j = 0; j < iterations; j++)
                    {
                        ids[IdUtils.NextId()] = true;
                    }
                });
            }

            long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            startGate.Set();
            Task.WaitAll(tasks);
            long end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            const int expectedSize = threadCount * iterations;
            Console.WriteLine($"Generated {ids.Count} in {end - start}ms");
            Assert.That(ids, Has.Count.EqualTo(expectedSize));
        }
    }
}
