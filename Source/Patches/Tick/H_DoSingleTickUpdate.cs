using HarmonyLib;
using Verse;

namespace Analyzer
{
    [HarmonyPatch(typeof(TickManager), nameof(TickManager.DoSingleTick))]
    internal class H_DoSingleTickUpdate
    {
        public static void Postfix()
        {
            if (GUIController.GetCurrentCategory == Category.Tick) // If we in Tick mode, finish our update (can happen multiple times p frame)
                Analyzer.EndUpdate();
        }
    }
}