using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Analyzer
{
    public static class Analyzer
    {
        private static int currentLogCount; // How many update cycles have passed since beginning profiling an entry?
        private static Thread logicThread; // Calculating stats for all active profilers (not the currently selected one)
        private static Thread patchingThread; // patching new methods, this prevents a stutter when patching mods
        private static Thread cleanupThread; // 'cleanup' - Removing patches, getting rid of cached methods (already patched), clearing temporary entries
        private static bool currentlyProfiling = false;
#if DEBUG
        private static bool midUpdate = false;
#endif
        private static float deltaTime = 0.0f;
        public static Dictionary<string, Profiler> profiles = new Dictionary<string, Profiler>();

        public static bool CurrentlyPaused { get; set; } = false;
        public static bool CurrentlyProfling => currentlyProfiling && !CurrentlyPaused;
        public static void RefreshLogCount() => currentLogCount = 0;
        public static int GetCurrentLogCount => currentLogCount;

        private static object patchingSync = new object();
        private static object logicSync = new object();

        public static Profiler Start(string key, Func<string> GetLabel = null, Type type = null, Def def = null, Thing thing = null, MethodBase meth = null)
        {
            if (CurrentlyProfling) return null;

            if (profiles.TryGetValue(key, out Profiler prof)) return prof.Start();
            else
            {
                if (GetLabel != null) profiles[key] = new Profiler(key, GetLabel(), type, def, thing, meth);
                else profiles[key] = new Profiler(key, key, type, def, thing, meth);

                return profiles[key].Start();
            }
        }
        public static void Stop(string key)
        {
            if (profiles.TryGetValue(key, out Profiler prof))
                prof.Stop();
        }

        // After this function has been called, the analyzer will be actively profiling / incuring lag :)
        public static void BeginProfiling() => currentlyProfiling = true;
        public static void EndProfiling() => currentlyProfiling = false;

        // Mostly here for book keeping, should be optimised out of a release build.
        public static void BeginUpdate()
        {
#if DEBUG
            if (CurrentlyProfling) return;

            if (midUpdate) Log.Error("[Analyzer] Attempting to begin new update cycle when the previous update has not ended");
            midUpdate = true;
#endif
        }

        public static void EndUpdate()
        {
            UpdateCycle(); // Update all our profilers, record measurements

            deltaTime += Time.deltaTime;
            if (deltaTime >= 1f)
            {
                FinishUpdateCycle(); // Process the information for all our profilers.
                deltaTime -= 1f;
            }
#if DEBUG
            midUpdate = false;
#endif
        }

        private static void UpdateCycle()
        {
            foreach (var profile in profiles)
                profile.Value.RecordMeasurement();
        }

        private static void FinishUpdateCycle()
        {
            // spawn a logic thread which will do our calcs
            // push to background
            // start
        }

        public static void PatchEntry(Entry entry)
        {
            lock (patchingSync) // If we are already patching something, we are just going to hang rimworld until it finishes :)
            {
                patchingThread = new Thread(() =>
                {
                    lock (patchingSync)
                    {
                        try
                        {
                            AccessTools.Method(entry.type, "ProfilePatch")?.Invoke(null, null);
                            entry.isPatched = true;
                        }
                        catch { }
                    }
                });
            }
            patchingThread.IsBackground = true;
            patchingThread.Start();
        }

        // Remove all patches
        // Clear all caches which hold information to prevent double patching
        // Clear all temporary entries
        // Clear all profiles
        // Clear all logs
        public static void Cleanup()
        {
            cleanupThread = new Thread(() => Modbase.UnPatchMethods());
            cleanupThread.IsBackground = true;
            cleanupThread.Start();
        }
    }
}
