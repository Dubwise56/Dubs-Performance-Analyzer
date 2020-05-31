using HarmonyLib;
using RimWorld;
using System.Reflection;

namespace DubsAnalyzer
{
    [ProfileMode("DoTabs", UpdateMode.GUI)]
    [HarmonyPatch(typeof(InspectPaneUtility), "DoTabs")]
    internal class H_DoTabs
    {
        public static bool Active=false;
        public static void Prefix(MethodBase __originalMethod, ref string __state)
        {
            if (Active)
            {
                __state = "InspectPaneUtility.DoTabs";
                Analyzer.Start(__state, null, null, null, null, __originalMethod as MethodInfo);
            }
        }

        public static void Postfix(string __state)
        {
            if (Active)
            {
                Analyzer.Stop(__state);
            }
        }
    }
}