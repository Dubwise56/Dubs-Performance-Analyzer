using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Verse;

namespace Analyzer
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

                for(int i = 0; i < currentLogCount; i++)
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
                logic.HighestTime = LocalTimes[0];
                logic.HighestCalls = LocalCalls[0];

                // general
                logic.Entries = currentLogCount;
                logic.OutlierCutoff = MovingWindowFiltered.OutlierThresholdFromData(LocalTimes.ToList(), 3, 3, 2);
                GetSpikes(ref logic.Spikes, LocalTimes, logic.MeanTimePerCall + logic.OutlierCutoff);


                lock (CurrentLogStats.sync) // Dump our current statistics into our static class which our drawing class uses
                {
                    CurrentLogStats.stats = logic;
                }
            }
            catch (Exception)
            {
#if DEBUG
                ThreadSafeLogger.Error("[Analyzer] Failed while calculating stats for profiler");
#endif
            }
        }

        public static void GetSpikes(ref List<double> spikes, double[] numbers, double cutoff)
        {
            foreach (double num in numbers)
                if (num > cutoff)
                    spikes.Add(num);
        }

    }


    public static class MovingWindowFiltered
    {
        public static double OutlierThresholdFromData(List<double> data, double reportStd, double outlierStd,
            double outlierMaxIterations)
        {
            List<double> filtered = data;

            // Perform outlier analysis
            for (int k = 0; k < outlierMaxIterations; k++)
            {
                double boundary = outlierStd * data.StandardDeviation();

                double average = data.Sum() / data.Count;
                double max = filtered.MaxBy(datum => Math.Abs(datum - average));
                if (Math.Abs(max - average) > boundary)
                {
                    filtered.Remove(max);
                }
                else
                {
                    break;
                }
            }

            return filtered.Sum() / filtered.Count + reportStd * filtered.StandardDeviation();
        }

        public static double StandardDeviation(this List<double> data)
        {
            double average = data.Sum() / data.Count;
            double deviations = data.Sum(datum => (datum - average) * (datum - average));
            return Math.Sqrt(deviations / data.Count);
        }
    }
}
