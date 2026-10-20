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
        public int ilIndex = -1;
#if V1_7
        public int vanillaLabel = -1;
#endif

        public int hitCounter = 0;

        public readonly double[] times;
        public readonly int[] hits;
        public readonly double[] childTimes;
        public long childTicks;
        public int seenIn;
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
            this.childTimes = new double[RECORDS_HELD];
            this.pinned = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start()
        {
            if (ProfileController.Gated) return;
            Push(this);
            stopwatch.Start();
            hitCounter++;
#if V1_7
            if (VanillaProfilerMirror.Active) VanillaProfilerMirror.Enter(this);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Stop()
        {
            stopwatch.Stop();
            Pop(this);
#if V1_7
            if (VanillaProfilerMirror.Used) VanillaProfilerMirror.Leave(this);
#endif
        }

        public void Reset()
        {
            stopwatch.Reset();
            hitCounter = 0;
            Array.Clear(times, 0, times.Length);
            Array.Clear(hits, 0, hits.Length);
            Array.Clear(childTimes, 0, childTimes.Length);
            childTicks = 0;
            seenIn = 0;
            Empty = true;
        }

        [ThreadStatic] private static Profiler[] stack;
        [ThreadStatic] private static long[] starts;
        [ThreadStatic] private static int depth;

        private static void Push(Profiler profiler)
        {
            stack ??= new Profiler[64];
            starts ??= new long[64];
            profiler.seenIn |= UnityData.IsInMainThread ? ProfileController.phase : ProfileController.PhaseThread;
            if (depth == stack.Length) return;
            stack[depth] = profiler;
            starts[depth++] = profiler.stopwatch.ElapsedTicks;
        }

        private static void Pop(Profiler profiler)
        {
            var i = depth - 1;
            while (i >= 0 && stack[i] != profiler) i--;
            if (i < 0) return;
            depth = i;
            if (i > 0 && stack[i - 1] != profiler) stack[i - 1].childTicks += Math.Max(0L, profiler.stopwatch.ElapsedTicks - starts[i]);
        }

        public static void ResetStack() => depth = 0;

        public void RecordMeasurement()
        {
#if DEBUG
            if (stopwatch.IsRunning) ThreadSafeLogger.Error($"[Analyzer] Profile {key} was still running when recorded");
#endif

            hits[currentIndex] = hitCounter;
            childTimes[currentIndex] = childTicks * 1000.0 / Stopwatch.Frequency;
            childTicks = 0;
            
            if (hitCounter != 0)
            {
                times[currentIndex] = stopwatch.Elapsed.TotalMilliseconds;
                stopwatch.Reset();
                hitCounter = 0;
                Empty = false;
            }
            else
            {
                times[currentIndex] = 0; // overwrite the time recorded one lap ago
            }

            currentIndex = (currentIndex + 1) % RECORDS_HELD; // ring buffer
        }

        public void CollectStatistics(int entries, out double average, out double max, out double total, out float calls, out float maxCalls)
        {
            total = 0;
            calls = 0;
            maxCalls = 0;
            max = 0;

            // we traverse backwards through the array, so when we reach -1
            // we wrap around back to the end
            uint arrayIndex = (currentIndex + RECORDS_HELD - 1) % RECORDS_HELD;
            int i = entries;

            while (i > 0)
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

        public double ChildAverage(int entries)
        {
            var total = 0d;
            var index = currentIndex;
            for (var i = 0; i < entries; i++)
            {
                index = (index + RECORDS_HELD - 1) % RECORDS_HELD;
                total += childTimes[index];
            }

            return total / entries;
        }

        public int FindSpikes(int entries, string units, out string text, out int count)
        {
            text = null;
            count = 0;
            var runs = new List<double>();
            var gaps = new Dictionary<int, int>();
            var last = -1;
            var worst = 0d;
            var index = currentIndex;
            for (var i = 0; i < entries; i++)
            {
                index = (index + RECORDS_HELD - 1) % RECORDS_HELD;
                if (hits[index] == 0) continue;

                runs.Add(times[index]);
                worst = Math.Max(worst, times[index]);
                if (last >= 0) gaps[i - last] = gaps.TryGetValue(i - last, out var n) ? n + 1 : 1;
                last = i;
            }

            if (runs.Count < 10) return 0;

            var gap = gaps.OrderByDescending(g => g.Value).FirstOrDefault();
            var interval = gap.Key > 1 && gap.Value >= (runs.Count - 1) * 0.8 ? gap.Key : 0;
            if (interval == 0 && worst < SpikeFloor) return 0;

            runs.Sort();
            var usual = runs[runs.Count / 2];
            var spikes = count = runs.Count(r => r > Math.Max(SpikeFloor, usual * 5));
            var every = interval > 0 ? Strings.spike_every(interval.ToString(), units) + " " : "";

            if (spikes > 0)
            {
                text = every + (spikes == 1 ? Strings.spike_one(entries.ToString(), units, worst.ToString("0.00"), usual.ToString("0.00")) : Strings.spike_many(spikes.ToString(), entries.ToString(), units, worst.ToString("0.00"), usual.ToString("0.00")))
                       + (interval > 0 ? " " + Strings.spike_notinterval : "");
                return 3;
            }

            if (interval == 0) return 0;

            if (usual >= HeavyRun)
            {
                text = every + Strings.spike_heavy(usual.ToString("0.00"));
                return 2;
            }

            text = every + Strings.spike_light(usual.ToString("0.00"));
            return 1;
        }

        public const double SpikeFloor = 1d, HeavyRun = 2d;
    }
}