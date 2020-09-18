using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;

namespace Analyzer.Profiling
{
    [Entry("ColonistBarOnGUI", Category.GUI)]
    internal class H_ColonistBarOnGUI
    {
        public static bool Active = false;

        public static IEnumerable<MethodInfo> GetPatchMethods() { yield return AccessTools.Method(typeof(ColonistBar), nameof(ColonistBar.ColonistBarOnGUI)); }
        public static string GetLabel() => "ColonistBar-OnGUI";
    }
}