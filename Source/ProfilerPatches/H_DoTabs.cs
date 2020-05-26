using HarmonyLib;
using RimWorld;

namespace DubsAnalyzer
{
    [ProfileMode("DoTabs", UpdateMode.GUI)]
    [HarmonyPatch(typeof(InspectPaneUtility), "DoTabs")]
    internal class H_DoTabs
    {
        public static bool Active = false;
        public static void Prefix(ref string __state)
        {
            if (Active)
            {
                __state = "InspectPaneUtility.DoTabs";
                Analyzer.Start(__state);
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