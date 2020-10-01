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
        public const int MAX_ADD_INFO_PER_FRAME = 250;
        public const int RECORDS_HELD = 2000;

        private readonly Watch stopwatch;
        public Type type;
        public Def def;
        public Thing thing;
        public MethodBase meth;

        public string label;
        public string key;

        public int hitCounter = 0;

        public readonly double[] times;
        public readonly int[] hits;
        public uint currentIndex = 0; // ring buffer tracking

        public Profiler(string key, string label, Type type, Def def, Thing thing, MethodBase meth)
        {
            this.key = key;
            this.thing = thing;
            this.def = def;
            this.meth = meth;
            this.label = label;
            this.stopwatch = new Watch();
            this.type = type;
            this.times = new double[RECORDS_HELD];
            this.hits = new int[RECORDS_HELD];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Profiler Start()
        {
            stopwatch.Start();
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Stop()
        {
            var adj = stopwatch.Stop();
            hitCounter++;
        }

        // This function will be added via transpiler to the end of `Stop()` when the option is toggled.
        // Hopefully this can be injected as IL, not an xtra method call.
        // Something.GetMethodIL();

        public static void StopFrameInfo(Profiler prof)
        {
            if (prof == GUIController.CurrentProfiler)
                if (prof.hitCounter < MAX_ADD_INFO_PER_FRAME)
                    StackTraceRegex.Add(new StackTrace(2, false));
        }

        public void RecordMeasurement()
        {
#if DEBUG
            if (stopwatch.IsRunning) ThreadSafeLogger.Error($"[Analyzer] Profile {key} was still running when recorded");
#endif

            times[currentIndex] = stopwatch.Elapsed.TotalMilliseconds;
            hits[currentIndex] = hitCounter;

            currentIndex++;
            currentIndex %= RECORDS_HELD; // ring buffer

            stopwatch.Reset();
            hitCounter = 0;
        }

        public void CollectStatistics(int entries, out double average, out double max, out double total, out float calls, out float maxCalls)
        {
            total = 0;
            average = 0;
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
                arrayIndex = (arrayIndex - 1) % RECORDS_HELD;
            }

            average = total / (float)entries;
        }
    }
}