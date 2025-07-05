using Analyzer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;
using Verse;

namespace Analyzer.Profiling
{
    public class Profiler
    {
        public const int RECORDS_HELD = 2000;

        private readonly Stopwatch stopwatch;
        public Type type;
        public MethodBase meth;

        public string label;
        public string key;
        public bool pinned;

        public int hitCounter = 0;

        public readonly double[] times;
        public readonly int[] hits;
        public uint currentIndex = 0; // ring buffer tracking
        public bool Empty = true;

        public Profiler(string key, string label, Type type, MethodBase meth)
        {
            this.key = key;
            this.meth = meth;
            this.label = label;
            this.stopwatch = new();
            this.type = type;
            this.times = new double[RECORDS_HELD];
            this.hits = new int[RECORDS_HELD];
            this.pinned = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start()
        {
            stopwatch.Start();
            hitCounter++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Stop()
        {
            stopwatch.Stop();
        }

        public void Reset()
        {
            stopwatch.Reset();
            hitCounter = 0;
            Array.Clear(times, 0, times.Length);
            Array.Clear(hits, 0, hits.Length);
            Empty = true;
        }

        public void RecordMeasurement()
        {
#if DEBUG
            if (stopwatch.IsRunning) ThreadSafeLogger.Error($"[Analyzer] Profile {key} was still running when recorded");
#endif

            hits[currentIndex] = hitCounter;
            
            if (hitCounter != 0)
            {
                times[currentIndex] = stopwatch.Elapsed.TotalMilliseconds;
                stopwatch.Reset();
                hitCounter = 0;
                Empty = false;
            }

            currentIndex = (currentIndex + 1) % RECORDS_HELD; // ring buffer
        }

        public void CollectStatistics(int entries, out double average, out double max, out double total, out float calls, out float maxCalls)
        {
            total = 0;
            calls = 0;
            maxCalls = hits[currentIndex];
            max = times[currentIndex];

            // we traverse backwards through the array, so when we reach -1
            // we wrap around back to the end
            uint arrayIndex = currentIndex;
            int i = entries;

            while (i >= 0)
            {
                var time = times[arrayIndex];
                var call = hits[arrayIndex];

                calls += call;
                total += time;
                if (time > max) max = time;
                if (call > maxCalls) maxCalls = call;

                i--;
                arrayIndex = arrayIndex - 1;
                if (arrayIndex > RECORDS_HELD) arrayIndex = RECORDS_HELD - 1;
            }

            average = total / (float) entries;

            if (calls == 0)
                Empty = true;
        }
    }
}