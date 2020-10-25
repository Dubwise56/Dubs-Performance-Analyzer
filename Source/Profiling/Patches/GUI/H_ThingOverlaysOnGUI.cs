using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Analyzer.Profiling
{
    [Entry("entry.gui.ThingOverlaysOnGUI", Category.GUI, "entry.gui.ThingOverlaysOnGUI.tooltip")]
    internal class H_ThingOverlaysOnGUI
    {
        public static bool Active = false;

        public static IEnumerable<MethodInfo> GetPatchMethods() { yield return AccessTools.Method(typeof(ThingOverlays), nameof(ThingOverlays.ThingOverlaysOnGUI)); }
        public static string GetLabel() => "ThingOverlaysOnGUI";
    }
}