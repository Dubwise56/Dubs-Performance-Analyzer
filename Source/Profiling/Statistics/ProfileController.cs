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
        public static float updateFrequency => 1 / Settings.updatesPerSecond; // how many ms per update (capped at every 0.05ms)

        public static ConcurrentDictionary<string, Profiler> Profiles => profiles;

        public static ConcurrentBag<GCHandle> Handles { get; } = [];

        private static Stopwatch rootProf = new Stopwatch();
        private static float prevSample = 0.0f;
        private static int hits = 0;
        public static float updateAverage;
        

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

        public static void Stop(string key)
        {
            if (Profiles.TryGetValue(key, out Profiler prof))
                prof.Stop();
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
            hits++;

            Analyzer.UpdateCycle(); // Update all our profilers, record measurements

            deltaTime += Time.deltaTime;
            if (deltaTime >= updateFrequency)
            {
                Analyzer.FinishUpdateCycle(); // Process the information for all our profilers.
                deltaTime -= updateFrequency;
            }

            if (Time.time - prevSample >= 1)
            {
                prevSample = Time.time;
                updateAverage = (rootProf.ElapsedMilliseconds / (float)hits);
                hits = 0;
                rootProf.Reset();
            }
            
            midUpdate = false;
        }
    }
}
