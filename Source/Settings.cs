using System.Collections.Generic;
using Analyzer.Performance;
using Analyzer.Profiling;
using UnityEngine;
using Verse;

namespace Analyzer
{
    public class Settings : ModSettings
    {
        public static Color timeColour = new Color32(79, 147, 191, 255);
        public static Color callsColour = new Color32(61, 200, 110, 255);
        public static Color GraphCol = new Color32(17, 17, 17, 255);

        // Developer settings
        public static string PathToDnspy = "";
        public static float updatesPerSecond = 2;
        public static bool verboseLogging;
        public static bool disableCleanup;
        public static bool disableTPSCounter;
        public static bool enableLog;
        public static HashSet<string> SavedPatches_Tick;
        public static HashSet<string> SavedPatches_Update;

        // Performance Settings are held in the type which implements the optimisation

        public override void ExposeData()
        {
            base.ExposeData();


            Scribe_Values.Look(ref GraphSettings.lineAliasing, "lineAliasing", 7.5f);
            Scribe_Values.Look(ref GraphSettings.showMax, "showMax");
            Scribe_Values.Look(ref GraphSettings.showAxis, "showAxis", true);
            Scribe_Values.Look(ref GraphSettings.showGrid, "showGrid", true);


            Scribe_Values.Look(ref timeColour, "timeColour", new Color32(79, 147, 191, 255));
            Scribe_Values.Look(ref callsColour, "callsColour", new Color32(10, 10, 255, 255));
            Scribe_Values.Look(ref GraphCol, "GraphCol", new Color32(17, 17, 17, 255));
            Scribe_Values.Look(ref PathToDnspy, "dnspyPath");
            Scribe_Values.Look(ref updatesPerSecond, "updatesPerSecond", 2);
            Scribe_Values.Look(ref verboseLogging, "verboseLogging");
            Scribe_Values.Look(ref disableCleanup, "disableCleanup");
            Scribe_Values.Look(ref disableTPSCounter, "disableTPSCounter");
            Scribe_Collections.Look(ref SavedPatches_Update, "SavedPatches_Update");
            Scribe_Collections.Look(ref SavedPatches_Tick, "SavedPatches_Tick");

            // We save/load all performance-related settings here.
            PerformancePatches.ExposeData();
        }

        public void DoSettings(Rect canvas)
        {
            if (Event.current.type == EventType.Layout) return;

            Panel_Settings.Draw(canvas, true);
        }
    }
}