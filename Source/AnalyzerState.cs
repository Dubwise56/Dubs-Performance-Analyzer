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
        Uninitialised, Patching, Open, UnpatchingQueued, Unpatching
    }

    public enum SideTabCategory
    {
        Home, ModderTools, Tick, Update, GUI, ModderAdded
    }

    public static class AnalyzerState
    {
        // Current State for the analyzer
        public static bool CurrentlyRunning; // is the GUI open
        public static CurrentState State = CurrentState.Uninitialised; // current status regarding patching profiles

        // 'Side Tabs'
        public static SideTabCategory CurrentSideTabCategory = SideTabCategory.Home;
        public static ProfileMode CurrentTab;

        // 'Profiles' - the content inside a Tab
        public static Dictionary<string, Profiler> CurrentProfiles = new Dictionary<string, Profiler>();
        public static string CurrentProfileKey = string.Empty;
        public static List<ProfileLog> Logs = new List<ProfileLog>();

        // 'Logs'
        public static bool HideStatistics = false;
        public static bool HideStacktrace = true;
        public static bool HideHarmony = false;

        public static List<ProfileTab> SideTabCategories = new List<ProfileTab>
        {
            new ProfileTab("Home", // category name
                () => { CurrentSideTabCategory = SideTabCategory.Home; }, // onclick action   
                () => CurrentSideTabCategory == SideTabCategory.Home, // how do we determine that this is selected
                UpdateMode.Dead, // update mode
                "Settings and utils"), // tooltip
            new ProfileTab("Modder Tools",  () => { CurrentSideTabCategory = SideTabCategory.ModderTools; },  () => CurrentSideTabCategory == SideTabCategory.ModderTools,     UpdateMode.Dead,         "Utilities for modders and advanced users to profile mods!"),
            new ProfileTab("Tick",          () => { CurrentSideTabCategory = SideTabCategory.Tick; },         () => CurrentSideTabCategory == SideTabCategory.Tick,            UpdateMode.Tick,         "Things that run on tick"),
            new ProfileTab("Update",        () => { CurrentSideTabCategory = SideTabCategory.Update; },       () => CurrentSideTabCategory == SideTabCategory.Update,          UpdateMode.Update,       "Things that run per frame"),
            new ProfileTab("GUI",           () => { CurrentSideTabCategory = SideTabCategory.GUI; },          () => CurrentSideTabCategory == SideTabCategory.GUI,             UpdateMode.GUI,          "Things that run on GUI"),
            new ProfileTab("Modder Added",  () => { CurrentSideTabCategory = SideTabCategory.ModderAdded; },  () => CurrentSideTabCategory == SideTabCategory.ModderAdded,     UpdateMode.ModderAdded,  "Categories that modders have added")
        };

        public static void ResetState()
        {
            Dialog_Analyzer.AdditionalInfo.Graph.reset();
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

        public static void MakeAndSwitchTab(string tabName, UpdateMode mode)
        {
            ProfileMode newMode = new ProfileMode(tabName, mode);
            newMode.typeRef = null;

            foreach (var profileTab in SideTabCategories)
            {
                if (mode == profileTab.UpdateMode)
                    profileTab.Modes.SetOrAdd(newMode, null);
            }

            SwapTab(new KeyValuePair<ProfileMode, Type>(newMode, null), mode);
        }
        
        public static bool CanPatch()
        {
            return (State == CurrentState.Open || State == CurrentState.Uninitialised || State == CurrentState.UnpatchingQueued);
        }
        public static bool CanCleanup()
        {
            return (State == CurrentState.Uninitialised || State == CurrentState.Open || State == CurrentState.Patching);
        }

        public static void SwapTab(string name, UpdateMode updateMode)
        {
            var cat = AnalyzerState.SideTabCategories.First(c => c.UpdateMode == updateMode);
            if (cat == null) return;

            var tab = cat.Modes.First(m => m.Key.name == name);
            if (tab.Key == null) return;

            SwapTab(tab, updateMode);
        }

        public static void SwapTab(KeyValuePair<ProfileMode, Type> mode, UpdateMode updateMode)
        {
            if (AnalyzerState.State == CurrentState.Uninitialised)
                Dialog_Analyzer.Reboot();
            
            if (!(CurrentSideTabCategory == UpdateToSideTab(updateMode)))
            {
                CurrentSideTabCategory = UpdateToSideTab(updateMode);
                Analyzer.Settings.Write();
            }

            if (CurrentTab != null)
                AnalyzerState.CurrentTab.SetActive(false);

            mode.Key.SetActive(true);
            CurrentTab = mode.Key;
            CurrentProfileKey = "Overview";
            ResetState();
                
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
                case UpdateMode.Dead:   return SideTabCategory.Home;
                case UpdateMode.Tick:   return SideTabCategory.Tick;
                case UpdateMode.Update: return SideTabCategory.Update;
                case UpdateMode.GUI:    return SideTabCategory.GUI;
            }
            return SideTabCategory.ModderAdded;
        }

    }
}
