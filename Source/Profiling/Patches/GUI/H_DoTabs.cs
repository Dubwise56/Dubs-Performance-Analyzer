using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;

namespace Analyzer.Profiling
{
    [Entry("entry.gui.dotabs", Category.GUI, "entry.gui.dotabs.tooltip")]
    internal class H_DoTabs
    {
        public static bool Active = false;

        public static IEnumerable<MethodInfo> GetPatchMethods() { yield return AccessTools.Method(typeof(InspectPaneUtility), nameof(InspectPaneUtility.DoTabs)); }
        public static string GetLabel() => "InspectPaneUtility - DoTabs";
    }
}