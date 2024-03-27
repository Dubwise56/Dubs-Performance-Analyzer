using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Analyzer.Profiling
{
    public static class CurrentLogStats // 'Current' stats that our drawing will access
    {
        public static object sync = new object();
        public static LogStats stats;
    }

    public class LogStats
    {
        public int Entries = -1;

        // Highests
        public int HighestCalls;
        public double HighestTime;
        public double MeanCallsPerUpdateCycle;
        public double MeanTimePerCall;

        // Mean
        public double MeanTimePerUpdateCycle;
        public int MedianCalls;

        // Median
        public double MedianTime;

        // General
        public int TotalCalls;

        // Total
        public double TotalTime;

        public void GenerateStats()
        {
            if (GUIController.CurrentProfiler == null)
                return;

            var logCount = Analyzer.GetCurrentLogCount;
            var curProf = GUIController.CurrentProfiler;

            var lTimes = new double[Profiler.RECORDS_HELD];
            var lCalls = new int[Profiler.RECORDS_HELD];

            Array.Copy(curProf.times, lTimes, Profiler.RECORDS_HELD);
            Array.Copy(curProf.hits, lCalls, Profiler.RECORDS_HELD);

            Task.Factory.StartNew(() => ExecuteWorker(lCalls, lTimes, logCount));
        }

        public static LogStats GatherStats(EntryFile file) {
            return GatherStats(file.calls, file.times, file.header.entries);
        }

        
        public static LogStats GatherStats(int[] calls, double[] times, int entries)
        {
            var stats = new LogStats();
            
            Array.Sort(calls);
            Array.Sort(times);
            
            for (var i = 0; i < entries; i++)
            {
                stats.TotalCalls += calls[i];
                stats.TotalTime += times[i];
            }

            // Mean
            stats.MeanTimePerCall = stats.TotalTime / stats.TotalCalls;
            stats.MeanTimePerUpdateCycle = stats.TotalTime / entries;
            stats.MeanCallsPerUpdateCycle = stats.TotalCalls / (float) entries;

            var middle = entries / 2;
            // Medians
            stats.MedianTime = times[middle];
            stats.MedianCalls = calls[middle];

            // Max
            stats.HighestTime = times[entries - 1];
            stats.HighestCalls = calls[entries - 1];

            // general
            stats.Entries = entries;

            return stats;
        }
        
        public static void ExecuteWorker(int[] LocalCalls, double[] LocalTimes, int currentLogCount)
        {
            try
            {
                // todo 
                // implement a custom sorting which also keeps track of the sum.
                // this will take this from
                // o(2*nlogn + n) to o(2*nlogn)

                var stats = GatherStats(LocalCalls, LocalTimes, currentLogCount);

                lock (CurrentLogStats.sync) // Dump our current statistics into our static class which our drawing class uses
                {
                    CurrentLogStats.stats = stats;
                }
            }
            catch (Exception e)
            {
#if DEBUG
                ThreadSafeLogger.ReportException(e, $"[Analyzer] Failed while calculating stats for profiler");
#else
                if(Settings.verboseLogging)
                    ThreadSafeLogger.ReportException(e, $"[Analyzer] Failed while calculating stats for profiler");
#endif
            }
        }
    }
}