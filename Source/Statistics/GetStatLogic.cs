using System;
using System.Collections.Generic;
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
    public static class CurrentStats // 'Current' stats that our drawing will access
    {
        public static object sync = new object();
        public static GetStatLogic stats = null;
    }


    public class GetStatLogic 
    {
        // Per Tick
        public float MeanCallsPerTick;
        public float MeanTimePerTick;

        // Per Call
        public float MeanTicksPerCall;
        public float MeanTimePerCall;
        public float StandardDeviation;
        public List<float> Spikes = new List<float>(); // above 3 standard deviations of the mean

        // Per Frame
        public float MeanTimeElapsedPerFrame;
        public float MeanCallsPerFrame;

        // Per Category 
        public float TimeInCategory; // as a percent (0.00%)

        // Highests
        public float MostCallsPerFrame = 0f;
        public float HighestTime = 0f;

        public float[] LocalTimes;
        public float[] LocalCalls;

        public static Thread thread = null;
        public static object sync = new object();
        public static bool IsActiveThread = false;

        public void GenerateStats(double[] times, int[] calls)
        {
            thread = new Thread(() => ExecuteWorker(this, times, calls));
            thread.Start();
        }

        private static void ExecuteWorker(GetStatLogic logic, double[] times, int[] calls)
        {
            IsActiveThread = true;
            Thread.CurrentThread.IsBackground = true;

            logic.LocalTimes = new float[times.Length];
            for(int i = 0; i < times.Length; i++)
                logic.LocalTimes[i] = (float)times[i];

            logic.LocalCalls = new float[calls.Length];
            for (int i = 0; i < calls.Length; i++)
                logic.LocalCalls[i] = (float)calls[i];

            float countTimes = logic.LocalTimes.Count();
            float countCalls = logic.LocalCalls.Count();

            float sumCalls = 0;
            float sumNotNull = 0;
            Sum(ref sumCalls, ref sumNotNull, logic.LocalCalls);

            float mean = GetMean(logic.LocalTimes, countTimes);

            float standardDeviation = GetStandardDeviation(mean, logic.LocalTimes, sumCalls);
            logic.StandardDeviation = standardDeviation;

            // calculate our 'spikes'
            float cutoff = mean + (3 * standardDeviation);
            GetSpikes(ref logic.Spikes, logic.LocalTimes, cutoff);

            //logic.CallsPerPeriod = sumCalls / sumNotNull;

            float callsMean = GetMean(logic.LocalCalls, sumNotNull);
            //logic.CallsPerTick = callsMean;

            lock (CurrentStats.sync) // Dump our current statistics into our static class which our drawing class uses
            {
                CurrentStats.stats = logic;
            }

            IsActiveThread = false;
        }

        public static void Sum(ref float sum, ref float sumNotNull, float[] numbers)
        {
            foreach(float num in numbers)
            {
                if (num > 0)
                    sumNotNull++;
                sum += num;
            }
        }
        public static float GetMean(float[] numbers, float count)
        {
            float mean = 0f;
            foreach (float num in numbers)
                mean += num;
            return mean / count;
        }
        public static float GetStandardDeviation(float mean, float[] numbers, float count)
        {
            float deviation = 0f;
            foreach(float num in numbers)
                deviation += ((num - mean) * (num - mean));
            return deviation / count;
        }
        public static void GetSpikes(ref List<float> spikes, float[] numbers, float cutoff)
        {
            foreach (float num in numbers)
                if (num > cutoff)
                    spikes.Add(num);
        }
    }
}
