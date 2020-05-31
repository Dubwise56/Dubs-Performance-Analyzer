using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace DubsAnalyzer
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
        public double StandardDeviation;
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
            //AnalyzerState.CurrentProfiler().History.times.CopyTo(lTimes, 0);
            if (AnalyzerState.CurrentProfiler() == null)
                return;

            for (int i = 0; i < 2000; i++)
                lTimes[i] = AnalyzerState.CurrentProfiler().History.times[i];
                
            //AnalyzerState.CurrentProfiler().History.hits.CopyTo(lCalls, 0);
            for (int i = 0; i < 2000; i++)
                lCalls[i] = AnalyzerState.CurrentProfiler().History.hits[i];

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
                logic.StandardDeviation = GetStandardDeviation(logic.MeanCallsPerFrame, LocalTimes, currentMaxIndex);
                GetSpikes(ref logic.Spikes, LocalTimes, logic.MeanTimePerCall + (3 * logic.StandardDeviation));


                lock (CurrentLogStats.sync) // Dump our current statistics into our static class which our drawing class uses
                {
                    CurrentLogStats.stats = logic;
                }


                IsActiveThread = false;

            } catch(Exception) { IsActiveThread = false; }
        }

        public static double GetStandardDeviation(double mean, double[] numbers, double count)
        {
            double deviation = 0f;
            for(int i = 0; i < count; i++)
                deviation += ((numbers[i] - mean) * (numbers[i] - mean));

            return (double) Mathf.Sqrt((float)deviation / (float)count);
        }
        public static void GetSpikes(ref List<double> spikes, double[] numbers, double cutoff)
        {
            foreach (double num in numbers)
                if (num > cutoff)
                    spikes.Add(num);
        }

    }
}
