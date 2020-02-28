using HarmonyLib;
using Verse;

namespace DubsAnalyzer
{
    [HarmonyPatch(typeof(TickManager), nameof(TickManager.DoSingleTick))]
    internal class H_DoSingleTickUpdate
    {
        public static void Postfix()
        {
            if (Analyzer.SelectedMode != null)
            {
                if (Analyzer.SelectedMode.mode == UpdateMode.Tick)
                    Analyzer.UpdateEnd();
            }
        }
    }
}