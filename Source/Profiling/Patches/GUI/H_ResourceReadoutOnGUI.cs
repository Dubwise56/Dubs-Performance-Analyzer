using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;

namespace Analyzer.Profiling
{
    [Entry("ResourceReadoutOnGUI", Category.GUI)]
    internal class H_ResourceReadoutOnGUI
    {
        public static bool Active = false;

        public static IEnumerable<MethodInfo> GetPatchMethods() { yield return AccessTools.Method(typeof(ResourceReadout), nameof(ResourceReadout.ResourceReadoutOnGUI)); }
        public static string GetLabel() => "ResourceReadoutOnGUI";
    }
}
