using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Verse;

namespace DubsAnalyzer
{
    public enum CurrentState
    {
        Unitialised, Patching, Open, UnpatchingQueued, Unpatching
    }

    public enum SideTabCategory
    {
        Home, ModderTools, Tick, Update, GUI
    }

    public static class AnalyzerState
    {
        // Current State for the analyzer
        public static bool CurrentlyRunning; // is the GUI open
        public static CurrentState State = CurrentState.Unitialised; // current status regarding patching profiles

        // 'Side Tabs'
        public static SideTabCategory CurrentSideTabCategory = SideTabCategory.Home;
        public static ProfileMode CurrentTab;

        // 'Profiles' the content inside a Tab
        public static Dictionary<string, Profiler> CurrentProfiles = new Dictionary<string, Profiler>();
        public static string CurrentProfileKey = string.Empty;
        public static List<ProfileLog> Logs = new List<ProfileLog>();

        public static void ResetState()
        {
            Dialog_Graph.reset();
            TabStats.reset();
            foreach (var profiler in CurrentProfiles)
                profiler.Value.Stop(false);

            CurrentProfiles.Clear();
            Logs.Clear();
        }

        public static Profiler CurrentProfiler()
        {
            if (CurrentProfiles.ContainsKey(CurrentProfileKey))
                return CurrentProfiles[CurrentProfileKey];

            return null;
        }
        public static Profiler GetProfile(string key)
        {
            if (CurrentProfiles.ContainsKey(key))
                return CurrentProfiles[key];

            return null;
        }
        public static bool HasProfile(string key)
        {
            return CurrentProfiles.ContainsKey(key);
        }

        public static bool CanPatch()
        {
            return (State == CurrentState.Open || State == CurrentState.Unitialised || State == CurrentState.UnpatchingQueued);
        }
        public static bool CanCleanup()
        {
            return (State == CurrentState.Unitialised || State == CurrentState.Open || State == CurrentState.Patching);
        }

        public static void SwapTab(KeyValuePair<ProfileMode, Type> mode, UpdateMode updateMode)
        {
            if (!(CurrentSideTabCategory == UpdateToSideTab(updateMode)))
            {
                CurrentSideTabCategory = UpdateToSideTab(updateMode);
                Analyzer.Settings.Write();
            }
            if (CurrentTab != null)
                AccessTools.Field(CurrentTab.typeRef, "Active").SetValue(null, false);

            AccessTools.Field(mode.Value, "Active").SetValue(null, true);
            CurrentTab = mode.Key;
            CurrentProfileKey = "Overview";
            Analyzer.Reset();

            if (!mode.Key.IsPatched)
                mode.Key.ProfilePatch();
        }


        /*
         * Utility
         */
        private static SideTabCategory UpdateToSideTab(UpdateMode updateMode)
        {
            switch (updateMode)
            {
                case UpdateMode.Dead: return SideTabCategory.Home;
                case UpdateMode.Tick: return SideTabCategory.Tick;
                case UpdateMode.Update: return SideTabCategory.Update;
            }
            return SideTabCategory.GUI;
        }

    }
}
