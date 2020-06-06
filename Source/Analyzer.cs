using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;
using UnityEngine;
using Verse;

namespace DubsAnalyzer
{
    [StaticConstructorOnStartup]
    public static class Harmy
    {
        static Harmy()
        {
            Analyzer.harmony = new Harmony("Dubwise.DubsProfiler");
            Analyzer.perfharmony = new Harmony("Dubwise.DubsOptimizer"); // makes sense

            H_KeyPresses.PatchMe();

            var modes = GenTypes.AllTypes.Where(m => m.TryGetAttribute<PerformancePatch>(out _));

            foreach (var mode in modes)
            {
                //AccessTools.Method(mode, "ProfilePatch")?.Invoke(null, null);
                AccessTools.Method(mode, "PerformancePatch")?.Invoke(null, new object[] { Analyzer.perfharmony });
            }

            //foreach (var workGiverDef in DefDatabase<WorkGiverDef>.AllDefsListForReading)
            //{
            //    if (workGiverDef.giverClass == typeof(WorkGiver_DoBill))
            //    {
            //        workGiverDef.giverClass = typeof(WorkGiver_DoBillFixed);
            //    }
            //}

            // GenCommandLine.Restart();
        }
    }

    public class Analyzer : Mod
    {
        public static Harmony harmony;
        public static Harmony perfharmony; // For all our performance patches, we don't want to accidentally unpatch them

        private const int NumSecondsForPatchClear = 30;

        public static readonly int MaxHistoryEntries = 3000;
        public static readonly int AveragingTime = 2000;
        public static readonly int UpdateInterval = 60;

        public static double AverageSum;
        public static PerfAnalSettings Settings;
        public static float delta = 0;
        public static string SortBy = "Usage";

        private static bool RequestStop;
        public static bool LogOpen = false;
        public static bool LogStack = false;

        public static Thread LogicThread = null;
        public static object sync = new object();

        public static Dictionary<string, List<MethodInfo>> methods = new Dictionary<string, List<MethodInfo>>();

        public Analyzer(ModContentPack content) : base(content)
        {
            Settings = GetSettings<PerfAnalSettings>();

            if (Settings.UnlockFramerate && Application.platform != RuntimePlatform.OSXPlayer)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 999;
            }

            foreach(var mod in LoadedModManager.RunningMods)
            {
                foreach (var ass in mod.assemblies.loadedAssemblies)
                    AnalyzerCache.AssemblyToModname.Add(new Tuple<string, string>(ass.FullName, mod.Name));
            }

            // Waiting on ProfilerPatches/ModderDefined.cs

            foreach(var dir in ModLister.AllActiveModDirs)
            {
                var count = dir.GetFiles("Analyzer.xml")?.Count();
                if (count != 0)
                {
                    var thing = dir.GetFiles("Analyzer.xml").First();
                    XmlDocument doc = new XmlDocument();
                    doc.Load(thing.OpenRead());
                    XmlParser.Parse(doc, ref methods);
                }
            }

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
            var newLogs = new List<ProfileLog>();
            foreach (var key in Profiles.Keys)
            {
                var value = Profiles[key];
                var av = value.History.GetAverageTime(AveragingTime);
                newLogs.Add(new ProfileLog(value.label, string.Empty, av, (float)value.History.times.Max(), null, key, string.Empty, 0, value.type, value.meth));
            }

            var dd = newLogs.Sum(x => x.Average);

            for (var index = 0; index < newLogs.Count; index++)
            {
                var k = newLogs[index];
                var pc = (float)(k.Average / dd);
                var Log = new ProfileLog(k.Label, pc.ToStringPercent(), pc, k.Max, k.Def, k.Key, k.Mod, pc, k.Type, k.Meth);
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

        public static void UpdateEnd()
        {
            if (!AnalyzerState.CurrentlyRunning || AnalyzerState.CurrentTab == null) return;

            LogOpen = Find.WindowStack.IsOpen(typeof(EditWindow_Log));

            foreach (Profiler profiler in AnalyzerState.CurrentProfiles.Values)
                profiler.RecordMeasurement();

            var ShouldUpdate = false;
            if (AnalyzerState.CurrentTab.mode == UpdateMode.Update || AnalyzerState.CurrentTab.mode == UpdateMode.GUI)
            {
                delta += Time.deltaTime;
                if (delta >= 1f)
                {
                    ShouldUpdate = true;
                    delta -= 1f;
                }
            }
            else if (AnalyzerState.CurrentTab.mode == UpdateMode.Tick)
            {
                ShouldUpdate = GenTicks.TicksGame % UpdateInterval == 0;
            }

            if (ShouldUpdate)
            {
                ShouldUpdate = false;
                LogicThread = new Thread(() => ThreadStart(AnalyzerState.CurrentProfiles));
                LogicThread.IsBackground = true;
                LogicThread.Start();
            }

            if (RequestStop)
            {
                AnalyzerState.CurrentlyRunning = false;
                RequestStop = false;
            }
        }

        public static void HotStart(string key, UpdateMode mode)
        {
            if (AnalyzerState.CurrentTab?.mode == mode)
                Start(key);
        }

        public static void HotStop(string key, UpdateMode mode)
        {
            if (AnalyzerState.CurrentTab?.mode == mode)
                Stop(key);
        }

        public static void Start(string key, Func<string> GetLabel = null, Type type = null, Def def = null, Thing thing = null, MethodInfo meth = null)
        {
            if (!AnalyzerState.CurrentlyRunning) return;

            try
            {
                AnalyzerState.CurrentProfiles[key].Start();
            }
            catch (Exception)
            {
                if (!AnalyzerState.CurrentProfiles.ContainsKey(key))
                {
                    if (GetLabel != null)
                        AnalyzerState.CurrentProfiles[key] = new Profiler(key, GetLabel(), type, def, thing, meth);
                    else
                        AnalyzerState.CurrentProfiles[key] = new Profiler(key, key, type, def, thing, meth);
                }
                AnalyzerState.CurrentProfiles[key].Start();
            }
        }

        public static void Stop(string key)
        {
            if (!AnalyzerState.CurrentlyRunning) return;

            try
            {
                if (LogOpen && Dialog_Analyzer.AdditionalInfo.Graph.key == key)
                    AnalyzerState.CurrentProfiles[key].Stop(true);
                else
                    AnalyzerState.CurrentProfiles[key].Stop(false);
            }
            catch (Exception) { }
        }

        public static void unPatchMethods(bool forceThrough = false)
        {
            Thread.CurrentThread.IsBackground = true;
            AnalyzerState.State = CurrentState.UnpatchingQueued;

            if (!forceThrough)
            {
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

            Log.Message("Beginning to unpatch methods");

            ClearState();
            harmony.UnpatchAll(harmony.Id);

            PatchUtils.UnpatchAllInternalMethods();

            Messages.Message("Dubs Performance Analyzer: Successfully finished unpatching methods", MessageTypeDefOf.NeutralEvent);

            AnalyzerState.State = CurrentState.Unitialised;
        }

        public static void ClearState()
        {
            foreach (var maintab in AnalyzerState.SideTabCategories)
                maintab.Modes.Clear();

            H_HarmonyPatches.PatchedPres = new List<Patch>();
            H_HarmonyPatches.PatchedPosts = new List<Patch>();
            H_HarmonyTranspilers.PatchedMeths = new List<MethodBase>();

            PatchUtils.PatchedTypes = new List<string>();
            PatchUtils.PatchedAssemblies = new List<string>();
            

            Reset();
        }

        //public static void DisplayTimerProperties()
        //{
        //    // Display the timer frequency and resolution.
        //    if (Stopwatch.IsHighResolution)
        //    {
        //        Log.Warning("Operations timed using the system's high-resolution performance counter.");
        //    }
        //    else
        //    {
        //        Log.Warning("Operations timed using the DateTime class.");
        //    }

        //    long frequency = Stopwatch.Frequency;
        //    Log.Warning($"  Timer frequency in ticks per second = {frequency}");
        //    long nanosecPerTick = (1000L * 1000L * 1000L) / frequency;
        //    Log.Warning($"  Timer is accurate within {nanosecPerTick} nanoseconds");
        //}
    }
}