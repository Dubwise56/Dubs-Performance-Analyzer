using RimWorld.QuestGen;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    public static class ProfileController
    {
        public static ConcurrentDictionary<string, Profiler> profiles = [];

        private static bool midUpdate = false;

        private static float deltaTime = 0.0f;
        private static int lastFrame = -1;
        public static float updateFrequency => 1 / Settings.updatesPerSecond; // how many ms per update (capped at every 0.05ms)

        public static ConcurrentDictionary<string, Profiler> Profiles => profiles;

        public static Profiler[] ILProfilers = new Profiler[256];
        private static int ilProfilerCount;
        private static readonly object ilProfilerLock = new();

        private static Stopwatch rootProf = new Stopwatch();
        private static readonly double[] rootTimes = new double[Profiler.RECORDS_HELD];
        private static uint rootIndex = 0;
        public static float updateAverage;
        public static long cycle;
        public static bool WholeFrameRoot => GUIController.CurrentCategory is Category.GUI or Category.Patches;

        public const int PhaseTick = 1, PhaseUpdate = 2, PhaseGUI = 4, PhaseThread = 8;
        public static int phase, gate;

        public static bool Gated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => gate != 0 && (phase & gate) == 0 && UnityData.IsInMainThread;
        }

        public static int GateFor(Category category) => category switch
        {
            Category.Tick => PhaseTick,
            Category.Update => PhaseUpdate | PhaseTick,
            Category.GUI => PhaseGUI,
            _ => 0
        };
        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Profiler Start(string key, Func<string> GetLabel = null, Type type = null, MethodBase meth = null)
        {
            if (!Analyzer.CurrentlyProfiling) return null;

            Profiler profiler;
            while (!Profiles.TryGetValue(key, out profiler))
            {
                if (Profiles.TryAdd(key, profiler = new(key, GetLabel != null ? GetLabel() : key, type, meth)))
                    break;
            }

            profiler.Start();
            return profiler;
        }

        public static int AddILProfiler(Profiler profiler)
        {
            lock (ilProfilerLock)
            {
                if (profiler.ilIndex >= 0) return profiler.ilIndex;

                if (ilProfilerCount == ILProfilers.Length)
                {
                    var grown = new Profiler[ILProfilers.Length * 2];
                    Array.Copy(ILProfilers, grown, ilProfilerCount);
                    ILProfilers = grown;
                }

                ILProfilers[ilProfilerCount] = profiler;
                return profiler.ilIndex = ilProfilerCount++;
            }
        }

        public static void ClearILProfilers()
        {
            lock (ilProfilerLock)
            {
                Array.Clear(ILProfilers, 0, ILProfilers.Length);
                ilProfilerCount = 0;
            }
        }

        public static void Stop(string key)
        {
            if (Profiles.TryGetValue(key, out Profiler prof))
                prof.Stop();
        }

        public static float CollectRootAverage(int entries)
        {
            double total = 0;
            var index = rootIndex;
            for (var i = 0; i < entries; i++)
            {
                index = (index + Profiler.RECORDS_HELD - 1) % Profiler.RECORDS_HELD;
                total += rootTimes[index];
            }

            return updateAverage = (float)(total / entries);
        }

        // Mostly here for book keeping, optimised out of a release build.
        public static void BeginUpdate()
        {
            if (Analyzer.CurrentlyPaused) return;
            
            rootProf.Start();

            if (midUpdate)
            {
                ThreadSafeLogger.Error("[CRITICAL] Caught analyzer trying to begin a new update cycle before finishing the previous one.");
            }

            midUpdate = true;
        }

        public static void EndUpdate()
        {
            if (Analyzer.CurrentlyPaused) return;
            
            rootProf.Stop();
            rootTimes[rootIndex] = WholeFrameRoot ? Time.unscaledDeltaTime * 1000.0 : rootProf.Elapsed.TotalMilliseconds;
            rootIndex = (rootIndex + 1) % Profiler.RECORDS_HELD;
            rootProf.Reset();

            Analyzer.UpdateCycle(); // Update all our profilers, record measurements
            cycle++;

            if (lastFrame != Time.frameCount)
            {
                lastFrame = Time.frameCount;
                deltaTime += Time.deltaTime;
            }
            if (deltaTime >= updateFrequency)
            {
                Analyzer.FinishUpdateCycle(); // Process the information for all our profilers.
                deltaTime -= updateFrequency;
            }
            
            midUpdate = false;
        }
    }
}
