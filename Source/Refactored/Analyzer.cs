using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        public static List<ProfileLog> logs = new List<ProfileLog>();
        private static Comparer<ProfileLog> logComparer = Comparer<ProfileLog>.Create((ProfileLog first, ProfileLog second) => first.average < second.average ? 1 : -1);

        private static Thread logicThread; // Calculating stats for all active profilers (not the currently selected one)
        private static Thread patchingThread; // patching new methods, this prevents a stutter when patching mods
        private static Thread cleanupThread; // 'cleanup' - Removing patches, getting rid of cached methods (already patched), clearing temporary entries

        private static object patchingSync = new object();
        private static object logicSync = new object();

        private static bool currentlyProfiling = false;

        public static List<ProfileLog> Logs => logs;
        public static object LogicLock => logicSync;

        public static bool CurrentlyPaused { get; set; } = false;
        public static bool CurrentlyProfling => currentlyProfiling && !CurrentlyPaused;

        public static void RefreshLogCount() => currentLogCount = 0;
        public static int GetCurrentLogCount => currentLogCount;

        // After this function has been called, the analyzer will be actively profiling / incuring lag :)
        public static void BeginProfiling() => currentlyProfiling = true;
        public static void EndProfiling() => currentlyProfiling = false;

        // Called every update period (tick / root update)
        internal static void UpdateCycle()
        {
            foreach (var profile in ProfileController.Profiles)
                profile.Value.RecordMeasurement();
        }

        // Called a variadic amount depending on the user settings, but most likely every 60 ticks / 1 second
        internal static void FinishUpdateCycle()
        {
            logicThread = new Thread(() => ProfileCalculations(new Dictionary<string, Profiler>(ProfileController.Profiles)));
            logicThread.IsBackground = true;
            logicThread.Start();
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

        public static void Cleanup()
        {
            cleanupThread = new Thread(() => CleanupBackground());
            cleanupThread.IsBackground = true;
            cleanupThread.Start();
        }

        private static void ProfileCalculations(Dictionary<string, Profiler> Profiles)
        {
            List<ProfileLog> newLogs = new List<ProfileLog>(Profiles.Count);

            double sumOfAverages = 0;

            foreach (var value in Profiles.Values)
            {
                double average = value.GetAverageTime(Mathf.Min(Analyzer.GetCurrentLogCount, 2000));
                newLogs.Add(new ProfileLog(value.label, string.Empty, average, (float)value.times.Max(), null, value.key, string.Empty, 0, value.type, value.meth));

                sumOfAverages += average;
            }

            List<ProfileLog> sortedLogs = new List<ProfileLog>(newLogs.Count);

            foreach (var log in newLogs)
            {
                float adjustedAverage = (float)(log.average / sumOfAverages);
                log.average = adjustedAverage;
                log.averageString = adjustedAverage.ToStringPercent();

                BinaryInsertion(sortedLogs, log);
            }

            // Swap our old logs with the new ones
            lock (LogicLock)
            {
                logs = sortedLogs;
            }
        }

        // Assume the array is currently sorted
        // We are looking for a position to insert a new entry
        private static void BinaryInsertion(List<ProfileLog> logs, ProfileLog value)
        {
            int index = Mathf.Abs(logs.BinarySearch(value, logComparer) + 1);

            logs.Insert(index, value);
        }

        // Remove all patches
        // Remove all internal patches
        // Clear all caches which hold information to prevent double patching
        // Clear all temporary entries
        // Clear all profiles
        // Clear all logs

        private static void CleanupBackground()
        {
            // idle for 30s chillin
            for (int i = 0; i < 30; i++)
            {
                Thread.Sleep(1000);
                if (CurrentlyProfling)
                    return;
            }

            // unpatch all methods
            Modbase.Harmony.UnpatchAll(Modbase.Harmony.Id);

            // clear all patches to prevent double patching
            Utility.ClearPatchedCaches();

            // clear all profiles
            ProfileController.Profiles.Clear();

            // clear all logs
            Analyzer.Logs.Clear();

            // call GC
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
