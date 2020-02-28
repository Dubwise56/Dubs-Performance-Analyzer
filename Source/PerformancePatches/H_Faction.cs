using HarmonyLib;
using RimWorld;

namespace DubsAnalyzer
{
    [PerformancePatch]
    internal class H_FactionManager
    {
        public static void PerformancePatch()
        {
            Analyzer.harmony.Patch(AccessTools.Method(typeof(FactionManager), nameof(FactionManager.RecacheFactions)),
                new HarmonyMethod(typeof(H_FactionManager), nameof(Prefix)));
        }

        public static void Prefix(FactionManager __instance)
        {
            for (var i = 0; i < __instance.allFactions.Count; i++)
            {
                if (__instance.allFactions[i].def == null)
                {
                    __instance.allFactions[i].def = FactionDef.Named("OutlanderCivil");
                }
            }
        }
    }
}