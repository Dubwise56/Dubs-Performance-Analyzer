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
        public double OutlierCutoff;
        public List<double> Spikes = new List<double>(); // above 3 standard deviations of the mean
        public int TotalCalls;

        // Total
        public double TotalTime;

        public void GenerateStats()
        {
            if (GUIController.CurrentProfiler == null)
                return;

            var logCount = Analyzer.GetCurrentLogCount;
            var curProf = GUIController.CurrentProfiler;
            var currentIndex = curProf.currentIndex;

            var lTimes = new double[Profiler.RECORDS_HELD];
            var lCalls = new int[Profiler.RECORDS_HELD];

            Array.Copy(curProf.times, lTimes, Profiler.RECORDS_HELD);
            Array.Copy(curProf.hits, lCalls, Profiler.RECORDS_HELD);

            Task.Factory.StartNew(() => ExecuteWorker(this, lCalls, lTimes, logCount, currentIndex));
        }

        private static void ExecuteWorker(LogStats logic, int[] LocalCalls, double[] LocalTimes, int currentLogCount,
            uint currentIndex)
        {
            try
            {
                // todo 
                // implement a custom sorting which also keeps track of the sum.
                // this will take this from
                // o(2*nlogn + n) to o(2*nlogn)

                Array.Sort(LocalCalls);
                Array.Sort(LocalTimes);


                for (var i = 0; i < Profiler.RECORDS_HELD; i++)
                {
                    logic.TotalCalls += LocalCalls[i];
                    logic.TotalTime += LocalTimes[i];
                }

                // Mean
                logic.MeanTimePerCall = logic.TotalTime / logic.TotalCalls;
                logic.MeanTimePerUpdateCycle = logic.TotalTime / currentLogCount;
                logic.MeanCallsPerUpdateCycle = logic.TotalCalls / (float) currentLogCount;

                var medianOffset = Profiler.RECORDS_HELD - currentLogCount;
                var middle = currentLogCount / 2;
                // Medians
                logic.MedianTime = LocalTimes[medianOffset + middle];
                logic.MedianCalls = LocalCalls[medianOffset + middle];

                // Max
                logic.HighestTime = LocalTimes[Profiler.RECORDS_HELD - 1];
                logic.HighestCalls = LocalCalls[Profiler.RECORDS_HELD - 1];

                // general
                logic.Entries = currentLogCount;

                lock (CurrentLogStats.sync
                ) // Dump our current statistics into our static class which our drawing class uses
                {
                    CurrentLogStats.stats = logic;
                }
            }
            catch (Exception e)
            {
#if DEBUG
                ThreadSafeLogger.Error(
                    $"[Analyzer] Failed while calculating stats for profiler, errored with the message {e.Message}");
#else
                if(Settings.verboseLogging)
                    ThreadSafeLogger.Error($"[Analyzer] Failed while calculating stats for profiler, errored with the message {e.Message}");
#endif
            }
        }
    }
}