using HarmonyLib;
using RimWorld;

namespace DubsAnalyzer
{
    [PerformancePatch]
    internal class H_CompDeepDrill
    {
        public static void PerformancePatch(Harmony harmony)
        {
            var skiff = AccessTools.Method(typeof(CompDeepDrill), nameof(CompDeepDrill.CanDrillNow));
            harmony.Patch(skiff, new HarmonyMethod(typeof(H_CompDeepDrill), nameof(Prefix)));
        }

        public static bool Prefix(CompDeepDrill __instance, ref bool __result)
        {
            if (!Analyzer.Settings.OptimizeDrills)
            {
                return true;
            }
            __result = (__instance.powerComp == null || __instance.powerComp.PowerOn) && (__instance.parent.Map.Biome.hasBedrock || __instance.ValuableResourcesPresent());
            return false;
        }
    }
}