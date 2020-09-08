using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        // Per Call
        public double MeanTimePerCall = 0;

        // Per Frame
        public double MeanCallsPerFrame = 0;
        public double MeanTimePerFrame = 0;

        // General
        public double OutlierCutoff;
        public List<double> Spikes = new List<double>(); // above 3 standard deviations of the mean
        public int Entries = -1;

        // Total
        public double TotalTime = 0;
        public double TotalCalls = 0;

        // Highests
        public double HighestCalls = 0f;
        public double HighestTime = 0f;

        public static Thread thread = null;
        public static bool IsActiveThread = false;

        public static int[] lCalls = new int[2000];
        public static double[] lTimes = new double[2000];

        public void GenerateStats()
        {
            if (GUIController.CurrentProfiler == null)
                return;

            GUIController.CurrentProfiler.times.CopyTo(lTimes, 0);
            GUIController.CurrentProfiler.hits.CopyTo(lCalls, 0);

            thread = new Thread(() => ExecuteWorker(this, lCalls, lTimes));
            thread.IsBackground = true;
            thread.Start();
        }

        private static void ExecuteWorker(LogStats logic, int[] LocalCalls, double[] LocalTimes)
        {
            try
            {
                IsActiveThread = true;

                // We need to find our 'current' location within the array. I.e. if we only have 300 entries, we shouldn't be averaging things assuming we have 2000 entries
                // We go backwards from the end of the array, until we find the very first value with an entry (not perfect, but until this is tracked inside the profiler thing we can't do better) TODO
                int currentMaxIndex = -1;
                for (int i = 1999; i >= 0; i--)
                {
                    if (LocalTimes[i] != 0 || LocalCalls[i] != 0)
                    {
                        currentMaxIndex = i;
                        break;
                    }
                }

                if (currentMaxIndex == -1)
                {
                    IsActiveThread = false;
                    return;
                }

                for (int i = 0; i < currentMaxIndex; i++)
                {
                    logic.TotalTime += LocalTimes[i];
                    if (logic.HighestTime < LocalTimes[i])
                        logic.HighestTime = LocalTimes[i];

                    logic.TotalCalls += LocalCalls[i];
                    if (logic.HighestCalls < LocalCalls[i])
                        logic.HighestCalls = LocalCalls[i];
                }

                // p/t
                logic.MeanTimePerCall = logic.TotalTime / logic.TotalCalls;

                // p/f
                logic.MeanTimePerFrame = logic.TotalTime / currentMaxIndex;
                logic.MeanCallsPerFrame = logic.TotalCalls / currentMaxIndex;

                // general
                logic.Entries = currentMaxIndex;
                logic.OutlierCutoff = MovingWindowFiltered.OutlierThresholdFromData(LocalTimes.ToList(), 3, 3, 2);
                GetSpikes(ref logic.Spikes, LocalTimes, logic.MeanTimePerCall + logic.OutlierCutoff);


                lock (CurrentLogStats.sync) // Dump our current statistics into our static class which our drawing class uses
                {
                    CurrentLogStats.stats = logic;
                }


                IsActiveThread = false;

            }
            catch (Exception) { IsActiveThread = false; }
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
