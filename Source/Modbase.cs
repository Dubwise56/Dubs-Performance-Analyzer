using Analyzer.Performance;
using Analyzer.Profiling;
using HarmonyLib;
using RimWorld;
using System;
using UnityEngine;
using Verse;
using StackTraceUtility = Analyzer.Profiling.StackTraceUtility;

namespace Analyzer 
{
    public class Modbase : Mod
    {
        public const int TIME_SINCE_CLOSE_FOR_CLEANUP = 30;

        public static Settings Settings;
        private static Harmony harmony = null;
        private static Harmony staticHarmony = null;
        public static Harmony Harmony => harmony;

        public static Harmony StaticHarmony => staticHarmony;

        // Major - Reworked functionality
        // Minor - New feature
        // Build - Change Existing Feature
        // Revision - Hotfix

        private static readonly Version analyzerVersion = new Version(1, 6, 0, 0);

        public static bool isPatched = false;
        public static bool visualExceptionIntegration = false;

        public Modbase(ModContentPack content) : base(content)
        {
            try
            {
                Settings = GetSettings<Settings>();

                ThreadSafeLogger.Message($"[Analyzer] Loaded version {analyzerVersion.Major}.{analyzerVersion.Minor}.{analyzerVersion.Build} rev {analyzerVersion.Revision}");

                staticHarmony = new Harmony("Dubwise.PerformanceAnalyzer");
                harmony = new Harmony("Dubwise.DubsProfiler");;

                if (ModLister.HasActiveModWithName("Visual Exceptions"))
                {
                    var type = AccessTools.TypeByName("VisualExceptions.ExceptionState");
                    var field = AccessTools.Field(type, "configuration");
                    type = AccessTools.TypeByName("VisualExceptions.Configuration");
                    var property = AccessTools.PropertyGetter(type, "Debugging");

                    visualExceptionIntegration = (bool) property.Invoke(field.GetValue(null), null);

                    var str = "Detected Visual Exceptions - " + (visualExceptionIntegration ? "Integrating" : "Is disabled, relying on inbuilt functionality");
                    ThreadSafeLogger.Message(str);
                }
                
                if(visualExceptionIntegration is false)
                {
                    // For registering harmony patches
                    StaticHarmony.Patch(AccessTools.Constructor(typeof(Harmony), new[] {typeof(string)}),
                        new HarmonyMethod(typeof(RememberHarmonyIDs), nameof(RememberHarmonyIDs.Prefix)));
                    
                    if(ModLister.HasActiveModWithName("HugsLib"))
                        StaticHarmony.Patch(AccessTools.Method("HugsLib.ModBase:ApplyHarmonyPatches"),
                            transpiler: new HarmonyMethod(typeof(RememberHarmonyIDs), nameof(RememberHarmonyIDs.Transpiler)));
                }

                {
                    // Profiling
                    ModInfoCache.PopulateCache(Content.Name);

                    GUIController.InitialiseTabs();

                    // GUI needs to be initialised before xml (the tabs need to exist for entries to be inserted into them)
                    XmlParser.CollectXmlData();

                    StackTraceUtility.Initialise();
                }

                {
                    // Always Running
                    StaticHarmony.Patch(
                        AccessTools.Method(typeof(GlobalControlsUtility),
                            nameof(GlobalControlsUtility.DoTimespeedControls)),
                        prefix: new HarmonyMethod(typeof(GUIElement_TPS), nameof(GUIElement_TPS.Prefix)));

                    var logError = AccessTools.Method(typeof(Log), nameof(Log.Error), new Type[] {typeof(string)});

                    StaticHarmony.Patch(logError,
                        prefix: new HarmonyMethod(typeof(DebugLogenabler), nameof(DebugLogenabler.ErrorPrefix)),
                        new HarmonyMethod(typeof(DebugLogenabler), nameof(DebugLogenabler.ErrorPostfix)));
                    StaticHarmony.Patch(AccessTools.Method(typeof(Prefs), "get_DevMode"),
                        prefix: new HarmonyMethod(typeof(DebugLogenabler), nameof(DebugLogenabler.DevModePrefix)));
                    StaticHarmony.Patch(AccessTools.Method(typeof(DebugWindowsOpener), "DevToolStarterOnGUI"),
                        prefix: new HarmonyMethod(typeof(DebugLogenabler), nameof(DebugLogenabler.DebugKeysPatch)));
                }

                {
                    // Performance Patches
                    PerformancePatches.InitialisePatches();
                }

#if DEBUG
                ThreadSafeLogger.Warning("==========================================================================");
                ThreadSafeLogger.Warning("                          Analyzer Running In Debug Mode                  ");
                ThreadSafeLogger.Warning("==========================================================================");
#endif
            }
            catch (Exception e)
            {
                ThreadSafeLogger.ReportException(e, "Failed to initialise analyzer, dumping messages to debug log");
            }
            finally
            {
                ThreadSafeLogger.DisplayLogs();
            }
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoSettings(inRect);
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            // Any patches we had pending closing are now going to get closed
            PerformancePatches.ClosePatches();
        }

        public override string SettingsCategory()
        {
            return "Dubs Performance Analyzer";
        }
    }
}
