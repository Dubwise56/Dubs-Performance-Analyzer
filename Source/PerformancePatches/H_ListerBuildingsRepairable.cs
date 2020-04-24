using HarmonyLib;
using RimWorld;
using Verse;

namespace DubsAnalyzer
{
    [PerformancePatch]
    internal class H_ListerBuildingsRepairable
    {
        public static void PerformancePatch()
        {
            Analyzer.harmony.Patch(
                AccessTools.Method(typeof(ListerBuildingsRepairable), nameof(ListerBuildingsRepairable.UpdateBuilding)),
                new HarmonyMethod(typeof(H_ListerBuildingsRepairable), nameof(Prefix)));
        }

        public static bool Prefix(Building b)
        {
            if (!Analyzer.Settings.FixRepair) return true;
            return b.def.building.repairable && b.def.useHitPoints;
        }
    }
}