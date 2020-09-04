using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;
using UnityEngine;
using Verse;

namespace Analyzer
{
    [StaticConstructorOnStartup]
    public static class Harmy
    {
        static Harmy()
        {
            Modbase.harmony = new Harmony("Dubwise.DubsProfiler");
        }
    }

    public class Modbase : Mod
    {
        public static Harmony harmony;

        private const int NumSecondsForPatchClear = 30;

        public static readonly int MaxHistoryEntries = 3000;
        public static readonly int AveragingTime = 2000;
        public static readonly int UpdateInterval = 60;

        public static double AverageSum;
        public static Settings Settings;
        public static float delta = 0;
        public static string SortBy = "Usage";

        private static bool RequestStop;
        public static bool LogOpen = false;
        public static bool LogStack = false;

        public static Thread LogicThread = null;
        public static object sync = new object();

        public static Dictionary<string, HashSet<MethodInfo>> methods = new Dictionary<string, HashSet<MethodInfo>>();

        public Modbase(ModContentPack content) : base(content)
        {
            Settings = GetSettings<Settings>();

            ModInfoCache.PopulateCache(Content.Name);
            XmlParser.CollectXmlData();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoSettings(inRect);
        }

        public override string SettingsCategory()
        {
            return "Dubs Performance Analyzer";
        }

        private static void ThreadStart(Dictionary<string, Profiler> Profiles)
        {
            List<ProfileLog> newLogs = new List<ProfileLog>();
            foreach (string key in Profiles.Keys)
            {
                Profiler value = Profiles[key];
                double av = value.History.GetAverageTime(AveragingTime);
                newLogs.Add(new ProfileLog(value.label, string.Empty, av, (float)value.History.times.Max(), null, key, string.Empty, 0, value.type, value.meth));
            }

            double total = newLogs.Sum(x => x.Average);

            for (int index = 0; index < newLogs.Count; index++)
            {
                ProfileLog k = newLogs[index];
                float pc = (float)(k.Average / total);
                ProfileLog Log = new ProfileLog(k.Label, pc.ToStringPercent(), pc, k.Max, k.Def, k.Key, k.Mod, pc, k.Type, k.Meth);
                newLogs[index] = Log;
            }

            newLogs.SortByDescending(x => x.Average);

            lock (sync)
            {
                AnalyzerState.Logs = newLogs;
            }
        }

        public static void Reset()
        {
            AnalyzerState.ResetState();
        }

        public static void StartProfiling()
        {
            AnalyzerState.CurrentlyRunning = true;
        }

        public static void StopProfiling()
        {
            RequestStop = true;
        }

        public static void UnPatchMethods(bool forceThrough = false)
        {
            Thread.CurrentThread.IsBackground = true;

            if (!forceThrough)
            {
                // this can occur if we 'force' unpatch
                if (AnalyzerState.State == CurrentState.Uninitialised)
                    return;

                AnalyzerState.State = CurrentState.UnpatchingQueued;

                for (int i = 0; i < NumSecondsForPatchClear; i++)
                {
                    // This should result in no overlap
                    // If there are issues regarding spamming open and close, you can lower thread
                    // sleep by x% and multiply NumSecondsForPatchClear to balance
                    // I.e. Thread.Sleep(500), i < NumSecondsForPatchClear * 2;
                    Thread.Sleep(1000);
                    if (AnalyzerState.State != CurrentState.UnpatchingQueued)
                        return;
                }
            }

            AnalyzerState.State = CurrentState.Unpatching;

            Log.Message("Analyzer: Beginning to unpatch methods");

            ClearState();
            harmony.UnpatchAll(harmony.Id);

            Utility.UnpatchAllInternalMethods();

            AnalyzerState.State = CurrentState.Uninitialised;

            if (forceThrough) // if not, this has already been done for us by the PostClose();
            {
                AnalyzerState.CurrentlyRunning = false;
                Settings.Write();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Messages.Message("Analyzer: Successfully finished unpatching methods", MessageTypeDefOf.NeutralEvent);
        }

        public static void ClearState()
        {
            try
            {
                foreach (ProfileTab maintab in AnalyzerState.SideTabCategories)
                {
                    List<KeyValuePair<Entry, Type>> removes = new List<KeyValuePair<Entry, Type>>();
                    maintab.Modes.Do(w =>
                    {
                        if (w.Key.Closable)
                        {
                            removes.Add(w);
                        }
                        else
                        {
                            w.Key.IsPatched = false;
                            w.Key.SetActive(false);
                            w.Key.Patchinator = null;
                        }
                    });

                    foreach (KeyValuePair<Entry, Type> val in removes) AnalyzerState.RemoveTab(val);
                }
            }
            catch { }

            H_HarmonyPatches.PatchedPres = new List<Patch>();
            H_HarmonyPatches.PatchedPosts = new List<Patch>();
            H_HarmonyTranspilers.PatchedMeths = new List<MethodBase>();

            Utility.PatchedTypes = new List<string>();
            Utility.PatchedAssemblies = new List<string>();

            AnalyzerState.CurrentTab = null;
            AnalyzerState.CurrentSideTabCategory = SideTabCategory.Home;

            AnalyzerState.CurrentProfileKey = "";
            AnalyzerState.CurrentLog = new ProfileLog();

            Reset();

        }

    }
}