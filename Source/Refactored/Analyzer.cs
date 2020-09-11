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
        private const int MAX_LOG_COUNT = 2000;
        private static int currentLogCount = 0; // How many update cycles have passed since beginning profiling an entry?
        public static List<ProfileLog> logs = new List<ProfileLog>();

        // todo, how can I do this more elegantly?
        private static Comparer<ProfileLog> maxComparer = Comparer<ProfileLog>.Create((ProfileLog first, ProfileLog second) => first.max < second.max ? 1 : -1);
        private static Comparer<ProfileLog> averageComparer = Comparer<ProfileLog>.Create((ProfileLog first, ProfileLog second) => first.average < second.average ? 1 : -1);
        private static Comparer<ProfileLog> percentComparer = Comparer<ProfileLog>.Create((ProfileLog first, ProfileLog second) => first.percent < second.percent ? 1 : -1);
        private static Comparer<ProfileLog> totalComparer = Comparer<ProfileLog>.Create((ProfileLog first, ProfileLog second) => first.total < second.total ? 1 : -1);
        private static Comparer<ProfileLog> callsComparer = Comparer<ProfileLog>.Create((ProfileLog first, ProfileLog second) => first.calls < second.calls ? 1 : -1);
        private static Comparer<ProfileLog> nameComparer = Comparer<ProfileLog>.Create((ProfileLog first, ProfileLog second) => string.Compare(first.label, second.label));

        private static Thread logicThread; // Calculating stats for all active profilers (not the currently selected one)
        private static Thread cleanupThread; // 'cleanup' - Removing patches, getting rid of cached methods (already patched), clearing temporary entries

        private static object patchingSync = new object();
        private static object logicSync = new object();

        private static bool currentlyProfiling = false;

        public static List<ProfileLog> Logs => logs;
        public static object LogicLock => logicSync;

        public static bool CurrentlyPaused { get; set; } = false;
        public static bool CurrentlyProfling => currentlyProfiling && !CurrentlyPaused;

        public static int GetCurrentLogCount => currentLogCount;

        // After this function has been called, the analyzer will be actively profiling / incuring lag :)
        public static void BeginProfiling() => currentlyProfiling = true;
        public static void EndProfiling() => currentlyProfiling = false;

        public static SortBy SortBy { get; set; } = SortBy.Percent;

        public static void RefreshLogCount()
        {
            currentLogCount = 0;
            lock (LogicLock)
            {
                logs.Clear();
            }
        }

        // Called every update period (tick / root update)
        internal static void UpdateCycle()
        {
            foreach (var profile in ProfileController.Profiles)
                profile.Value.RecordMeasurement();

            if (currentLogCount < MAX_LOG_COUNT)
                currentLogCount++;
        }

        // Called a variadic amount depending on the user settings, but most likely every 60 ticks / .5 second
        internal static void FinishUpdateCycle()
        {
            if (ProfileController.Profiles.Count != 0)
            {
                Comparer<ProfileLog> comparer = percentComparer;
                switch (SortBy)
                {
                    case SortBy.Max: comparer = maxComparer; break;
                    case SortBy.Average: comparer = averageComparer; break;
                    case SortBy.Percent: comparer = percentComparer; break;
                    case SortBy.Total: comparer = totalComparer; break;
                    case SortBy.Calls: comparer = callsComparer; break;
                    case SortBy.Name: comparer = nameComparer; break;
                }

                logicThread = new Thread(() => ProfileCalculations(new Dictionary<string, Profiler>(ProfileController.Profiles), currentLogCount, comparer));
                logicThread.IsBackground = true;
                logicThread.Start();
            }
        }

        public static void PatchEntry(Entry entry)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    AccessTools.Method(entry.type, "ProfilePatch")?.Invoke(null, null);
                    entry.isLoading = false;
                    entry.isPatched = true;
                }
                catch (Exception e)
                {
#if DEBUG
                    ThreadSafeLogger.Error($"[Analyzer] Failed to patch entry, failed with the message {e.Message}");
#endif
#if NDEBUG
                    if (Settings.verboseLogging)
                        ThreadSafeLogger.Error($"[Analyzer] Failed to patch entry, failed with the message {e.Message}");
#endif
                }
            });
        }

        public static void Cleanup()
        {
            cleanupThread = new Thread(() => CleanupBackground());
            cleanupThread.IsBackground = true;
            cleanupThread.Start();
        }

        private static void ProfileCalculations(Dictionary<string, Profiler> Profiles, int currentLogCount, Comparer<ProfileLog> comparer)
        {
            List<ProfileLog> newLogs = new List<ProfileLog>(Profiles.Count);

            double sumOfAverages = 0;

            foreach (var value in Profiles.Values)
            {
                value.CollectStatistics(Mathf.Min(currentLogCount, MAX_LOG_COUNT - 1), out var average, out var max, out var total, out var calls);
                newLogs.Add(new ProfileLog(value.label, string.Empty, average, (float)max, null, value.key, string.Empty, 0, (float)total, calls, value.type, value.meth));

                sumOfAverages += average;
            }

            List<ProfileLog> sortedLogs = new List<ProfileLog>(newLogs.Count);

            foreach (var log in newLogs)
            {
                float adjustedAverage = (float)(log.average / sumOfAverages);
                log.percent = adjustedAverage;
                log.percentString = adjustedAverage.ToStringPercent();

                BinaryInsertion(sortedLogs, log, comparer);
            }

            // Swap our old logs with the new ones
            lock (LogicLock)
            {
                logs = sortedLogs;
            }
        }

        // Assume the array is currently sorted
        // We are looking for a position to insert a new entry
        private static void BinaryInsertion(List<ProfileLog> logs, ProfileLog value, Comparer<ProfileLog> comparer)
        {
            int index = Mathf.Abs(logs.BinarySearch(value, comparer) + 1);

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
                // Reads and writes of the following data types are atomic: bool, char, byte, sbyte, short, ushort, uint, int, float, and reference types. as found in C# Language Spec.
                if (currentlyProfiling) // atomic, doesn't need a lock
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

            // clear all temp entries
            GUIController.ClearEntries();

            // call GC
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // atomic reads and writes.
            Modbase.isPatched = false;

#if DEBUG 
            ThreadSafeLogger.Message($"Finished state cleanup");
#endif
        }
    }
}
