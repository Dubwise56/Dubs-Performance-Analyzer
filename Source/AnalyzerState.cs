using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Analyzer
{
    public static class AnalyzerState
    {
        //// Current State for the analyzer
        //public static bool CurrentlyRunning; // is the GUI open
        //public static CurrentState State = CurrentState.Uninitialised; // current status regarding patching profiles

        //// 'Side Tabs'
        //public static SideTabCategory CurrentSideTabCategory = SideTabCategory.Home;
        //public static Entry CurrentTab;

        //// 'Profiles' 
        //public static Dictionary<string, Profiler> CurrentProfiles = new Dictionary<string, Profiler>();
        //public static string CurrentProfileKey = string.Empty;
        //public static ProfileLog CurrentLog = new ProfileLog();
        //public static List<ProfileLog> Logs = new List<ProfileLog>();

        //// GUI
        //public static bool HideStatistics = false;
        //public static bool HideStacktrace = true;
        //public static bool HideHarmony = false;

        //// Type Cache
        //public static HashSet<string> types = new HashSet<string>();

        //public static void ResetState()
        //{
        //    Dialog_Analyzer.AdditionalInfo.Graph.reset();
        //    foreach (KeyValuePair<string, Profiler> profiler in CurrentProfiles)
        //        profiler.Value.Stop();

        //    CurrentProfiles.Clear();

        //    lock (Modbase.sync)
        //    {
        //        Logs.Clear();
        //    }
        //}

        //public static void MakeAndSwitchTab(string tabName)
        //{
        //    Type myType = null;
        //    if (types.Contains(tabName))
        //    {
        //        myType = AccessTools.TypeByName(tabName);
        //    }
        //    else
        //    {
        //        myType = DynamicTypeBuilder.CreateType(tabName, null);
        //        types.Add(tabName);
        //    }

        //    foreach (ProfileTab profileTab in SideTabCategories)
        //    {
        //        if (profileTab.UpdateMode == UpdateMode.Update)
        //        {
        //            Entry mode = Entry.Create(myType.Name, UpdateMode.Update, null, false, myType, true);

        //            mode.IsPatched = true;
        //            mode.SetActive(true);
        //            profileTab.Modes.Add(mode, myType);
        //            break;
        //        }
        //    }

        //    SwapTab(tabName, UpdateMode.Update);
        //}

        //public static void RemoveTab(KeyValuePair<Entry, Type> tab)
        //{
        //    foreach (ProfileTab profileTab in SideTabCategories)
        //    {
        //        IEnumerable<KeyValuePair<Entry, Type>> deletedTab = profileTab.Modes.Where(t => t.Key == tab.Key);
        //        if (deletedTab == null || deletedTab.Count() == 0 || deletedTab.First().Key == null) continue;

        //        profileTab.Modes.Remove(deletedTab.First().Key);

        //        if (deletedTab == CurrentTab)
        //            CurrentTab = profileTab.Modes.First().Key;

        //        return;
        //    }
        //}
        //public static bool CanPatch()
        //{
        //    return (State == CurrentState.Open || State == CurrentState.Uninitialised || State == CurrentState.UnpatchingQueued);
        //}
        //public static bool CanCleanup()
        //{
        //    return (State == CurrentState.Uninitialised || State == CurrentState.Open || State == CurrentState.Patching);
        //}

        //public static void SwapTab(string name)
        //{
        //    foreach (ProfileTab cat in SideTabCategories)
        //    {
        //        IEnumerable<KeyValuePair<Entry, Type>> tab = cat.Modes.Where(m => m.Key.name == name);
        //        if (tab == null || tab.Count() == 0 || tab.First().Key == null) return;

        //        SwapTab(tab.First(), cat.UpdateMode);
        //    }
        //}

        //public static void SwapTab(string name, UpdateMode updateMode)
        //{
        //    ProfileTab cat = SideTabCategories.First(c => c.UpdateMode == updateMode);
        //    if (cat == null) return;

        //    IEnumerable<KeyValuePair<Entry, Type>> tab = cat.Modes.Where(m => m.Key.name == name);
        //    if (tab == null || tab.Count() == 0 || tab.First().Key == null) return;

        //    SwapTab(tab.First(), updateMode);
        //}

        //public static void SwapTab(KeyValuePair<Entry, Type> mode, UpdateMode updateMode)
        //{
        //    if (State == CurrentState.Uninitialised)
        //        Dialog_Analyzer.Reboot();

        //    if (!(CurrentSideTabCategory == UpdateToSideTab(updateMode)))
        //    {
        //        CurrentSideTabCategory = UpdateToSideTab(updateMode);
        //        Modbase.Settings.Write();
        //    }

        //    if (CurrentTab != null)
        //        CurrentTab.SetActive(false);

        //    mode.Key.SetActive(CurrentlyRunning);
        //    CurrentTab = mode.Key;
        //    CurrentProfileKey = "Overview";
        //    ResetState();

        //    if (!mode.Key.IsPatched)
        //        mode.Key.ProfilePatch();
        //}

    }
}
