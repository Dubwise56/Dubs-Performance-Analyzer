using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace Analyzer.Profiling
{
    public static class CurrentLogStats // 'Current' stats that our drawing will access
    {
        public static object sync = new object();
        public static LogStats stats = null;
    }


    public class LogStats
    {
        // General
        public double OutlierCutoff;
        public List<double> Spikes = new List<double>(); // above 3 standard deviations of the mean
        public int Entries = -1;

        // Total
        public double TotalTime = 0;
        public int TotalCalls = 0;

        // Highests
        public int HighestCalls = 0;
        public double HighestTime = 0f;

        // Mean
        public double MeanTimePerUpdateCycle = 0;
        public double MeanCallsPerUpdateCycle = 0;
        public double MeanTimePerCall = 0;

        // Median
        public double MedianTime = 0;
        public int MedianCalls = 0;


        public void GenerateStats()
        {
            if (GUIController.CurrentProfiler == null)
                return;

            int logCount = Analyzer.GetCurrentLogCount;
            var curProf = GUIController.CurrentProfiler;
            uint currentIndex = curProf.currentIndex;

            var lTimes = new double[logCount];
            var lCalls = new int[logCount];

            Array.Copy(curProf.times, lTimes, logCount);
            Array.Copy(curProf.hits, lCalls, logCount);

            Task.Factory.StartNew(() => ExecuteWorker(this, lCalls, lTimes, logCount));
        }

        private static void ExecuteWorker(LogStats logic, int[] LocalCalls, double[] LocalTimes, int currentLogCount)
        {
            try
            {
                // todo 
                // implement a custom sorting which also keeps track of the sum.
                // this will take this from
                // o(2*nlogn + n) to o(2*nlogn)

                Array.Sort(LocalCalls);
                Array.Sort(LocalTimes);
                    

                for (int i = 0; i < currentLogCount; i++)
                {
                    logic.TotalCalls += LocalCalls[i];
                    logic.TotalTime += LocalTimes[i];
                }

                // Mean
                logic.MeanTimePerCall = logic.TotalTime / logic.TotalCalls;
                logic.MeanTimePerUpdateCycle = logic.TotalTime / currentLogCount;
                logic.MeanCallsPerUpdateCycle = logic.TotalCalls / currentLogCount;

                // Medians
                logic.MedianTime = LocalTimes[currentLogCount / 2];
                logic.MedianCalls = LocalCalls[currentLogCount / 2];

                // Max
                logic.HighestTime = LocalTimes[currentLogCount - 1];
                logic.HighestCalls = LocalCalls[currentLogCount - 1];

                // general
                logic.Entries = currentLogCount;

                lock (CurrentLogStats.sync) // Dump our current statistics into our static class which our drawing class uses
                {
                    CurrentLogStats.stats = logic;
                }
            }
            catch (Exception e)
            {
#if DEBUG
                ThreadSafeLogger.Error($"[Analyzer] Failed while calculating stats for profiler, errored with the message {e.Message}");
#endif
                #if NDEBUG
                    if(Settings.verboseLogging)
                        ThreadSafeLogger.Error($"[Analyzer] Failed while calculating stats for profiler, errored with the message {e.Message}");
                #endif
            }
        }
    }

}
