using HarmonyLib;
using Verse;

namespace DubsAnalyzer
{
    [HarmonyPatch(typeof(TickManager), nameof(TickManager.DoSingleTick))]
    internal class H_DoSingleTickUpdate
    {
        public static void Postfix()
        {
            if (AnalyzerState.CurrentTab != null)
            {
                if (AnalyzerState.CurrentTab.mode == UpdateMode.Tick)
                    Analyzer.UpdateEnd();
            }
        }
    }
}