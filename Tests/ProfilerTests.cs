using System.Diagnostics;
using System.Linq;
using Analyzer.Profiling;
using Xunit;

namespace Analyzer.Tests
{
    public class ProfilerTests
    {
        private const int Window = Profiler.RECORDS_HELD - 1; // what Analyzer.ProfileCalculations passes once full

        private static Profiler NewProfiler() => new("key", "label", null, null);

        // Busy-wait so the profiler's Stopwatch records a non-zero time.
        private static void TimedCall(Profiler profiler)
        {
            profiler.Start();
            var watch = Stopwatch.StartNew();
            while (watch.Elapsed.TotalMilliseconds < 0.05) { }
            profiler.Stop();
        }

        private static void IdleCycles(Profiler profiler, int count)
        {
            for (var cycle = 0; cycle < count; cycle++)
                profiler.RecordMeasurement();
        }

        [Fact]
        public void CallOutsideWindow_DoesNotCountTowardTotal()
        {
            var profiler = NewProfiler();
            TimedCall(profiler);
            profiler.RecordMeasurement();

            IdleCycles(profiler, Profiler.RECORDS_HELD);

            profiler.CollectStatistics(Window, out var average, out var max, out var total, out var calls, out _);
            Assert.Equal(0f, calls);
            Assert.Equal(0.0, total);
            Assert.Equal(0.0, average);
            Assert.Equal(0.0, max);
        }

        [Fact]
        public void InfrequentCalls_TimesMatchCallsInWindow()
        {
            // One call per lap, at a different slot each lap, as with hash-staggered tick intervals.
            var profiler = NewProfiler();
            const int laps = 10;
            for (var lap = 0; lap < laps; lap++)
            {
                for (var cycle = 0; cycle < Profiler.RECORDS_HELD; cycle++)
                {
                    if (cycle == lap * 7)
                        TimedCall(profiler);
                    profiler.RecordMeasurement();
                }
            }

            profiler.CollectStatistics(Window, out _, out _, out _, out var calls, out _);
            Assert.Equal(1f, calls);
            Assert.Equal(1, profiler.times.Count(time => time > 0));
        }
    }
}
