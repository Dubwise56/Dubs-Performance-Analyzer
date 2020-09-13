using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using UnityEngine;
using Verse;

namespace Analyzer
{
    [StaticConstructorOnStartup]
    public class Gfx
    {
        public static Texture2D disco = ContentFinder<Texture2D>.Get("DPA/UI/discord", false);
        public static Texture2D Support = ContentFinder<Texture2D>.Get("DPA/UI/Support", false);
    }

    public class Settings : ModSettings
    {

        public static string methToPatch = string.Empty;

        public static List<MethodInfo> GotMeth = new List<MethodInfo>();
        public Color LineCol = new Color32(79, 147, 191, 255);
        public Color GraphCol = new Color32(17, 17, 17, 255);
        public static string MethSearch = string.Empty;
        public static string TypeSearch = string.Empty;

        // Developer settings
        public static string @PathToDnspy = "";
        public static float updatesPerSecond = 2;
        public static bool verboseLogging = false;
        public static bool sidePanel = false;

#if DEBUG
        // Debug settings
        public static bool showGrapplingBoxes = false;
#endif

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref LineCol, "LineCol", new Color32(79, 147, 191, 255));
            Scribe_Values.Look(ref GraphCol, "GraphCol", new Color32(17, 17, 17, 255));
            Scribe_Values.Look(ref PathToDnspy, "dnspyPath");
            Scribe_Values.Look(ref updatesPerSecond, "updatesPerSecond", 2);
            Scribe_Values.Look(ref verboseLogging, "verboseLogging", false);
            Scribe_Values.Look(ref sidePanel, "sidePanel", false);

#if DEBUG
            Scribe_Values.Look(ref showGrapplingBoxes, "grapplingBoxes", false);
#endif
        }

        public void DoSettings(Rect canvas)
        {
            if (Event.current.type == EventType.Layout) return;

            Panel_Settings.Draw(canvas);
        }
    }
}
