using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Remoting;
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

        public static Dictionary<string, HashSet<MethodInfo>> methods = new Dictionary<string, HashSet<MethodInfo>>();

        public Analyzer(ModContentPack content) : base(content)
        {
            Settings = GetSettings<PerfAnalSettings>();

            if (Settings.UnlockFramerate && Application.platform != RuntimePlatform.OSXPlayer)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 999;
            }

            foreach (var mod in LoadedModManager.RunningMods)
            {
                foreach (var ass in mod.assemblies.loadedAssemblies)
                    AnalyzerCache.AssemblyToModname.Add(new Tuple<string, string>(ass.FullName, mod.Name));
            }

            // Waiting on ProfilerPatches/ModderDefined.cs

            foreach (var dir in ModLister.AllActiveModDirs)
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

            if (Analyzer.methods.Count != 0)
            {
                foreach (var m in Analyzer.methods)
                {
                    Type myType = DynamicTypeBuilder.CreateType(m.Key, m.Value);

                    foreach (var profileTab in AnalyzerState.SideTabCategories)
                    {
                        if (profileTab.UpdateMode == UpdateMode.ModderAdded)
                        {
                            ProfileMode mode = ProfileMode.Create(myType.Name, UpdateMode.ModderAdded, null, false, myType);
                            profileTab.Modes.Add(mode, null);
                            break;
                        }
                    }
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

            var total = newLogs.Sum(x => x.Average);

            for (var index = 0; index < newLogs.Count; index++)
            {
                var k = newLogs[index];
                var pc = (float)(k.Average / total);
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
            if (AnalyzerState.CurrentTab.mode == UpdateMode.Update || AnalyzerState.CurrentTab.mode == UpdateMode.GUI || AnalyzerState.CurrentTab.mode == UpdateMode.ModderAdded)
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
                LogicThread = new Thread(() => ThreadStart(new Dictionary<string, Profiler>(AnalyzerState.CurrentProfiles)));
                LogicThread.IsBackground = true;
                LogicThread.Start();
            }

            if (RequestStop)
            {
                AnalyzerState.CurrentlyRunning = false;
                RequestStop = false;
            }
        }

        public static Profiler Start(string key, Func<string> GetLabel = null, Type type = null, Def def = null, Thing thing = null, MethodBase meth = null)
        {
            if (!AnalyzerState.CurrentlyRunning) return null;

            try
            {
                var prof = AnalyzerState.CurrentProfiles[key];
                prof.Start();
                return prof;
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
                var prof = AnalyzerState.CurrentProfiles[key];
                prof.Start();
                return prof;
            }
        }

        public static void Stop(string key)
        {
            try
            {
                AnalyzerState.CurrentProfiles[key].Stop();
            }
            catch (Exception) { }
        }

        public static void unPatchMethods(bool forceThrough = false)
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

            Log.Message("Beginning to unpatch methods");

            ClearState();
            harmony.UnpatchAll(harmony.Id);

            PatchUtils.UnpatchAllInternalMethods();

            AnalyzerState.State = CurrentState.Uninitialised;

            if (forceThrough) // if not, this has already been done for us by the PostClose();
            {
                Analyzer.StopProfiling();
                Analyzer.Reset();
                Analyzer.Settings.Write();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Messages.Message("Dubs Performance Analyzer: Successfully finished unpatching methods", MessageTypeDefOf.NeutralEvent);
        }

        public static void ClearState()
        {
            foreach (var maintab in AnalyzerState.SideTabCategories)
            {
                List<KeyValuePair<ProfileMode, Type>> removes = new List<KeyValuePair<ProfileMode, Type>>();
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

                foreach (var val in removes)
                    AnalyzerState.RemoveTab(val);
            }



            H_HarmonyPatches.PatchedPres = new List<Patch>();
            H_HarmonyPatches.PatchedPosts = new List<Patch>();
            H_HarmonyTranspilers.PatchedMeths = new List<MethodBase>();

            PatchUtils.PatchedTypes = new List<string>();
            PatchUtils.PatchedAssemblies = new List<string>();

            AnalyzerState.CurrentTab = null;
            AnalyzerState.CurrentSideTabCategory = SideTabCategory.Home;

            Reset();
        }

    }
}