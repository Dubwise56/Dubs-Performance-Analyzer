using HarmonyLib;
using UnityEngine;
using Verse;

namespace DubsAnalyzer
{
    [PerformancePatch]
    internal class H_SectionLayer_Snow
    {
        public static void PerformancePatch(Harmony harmony)
        {
            var skiff = AccessTools.Method(typeof(SectionLayer_Snow), nameof(SectionLayer_Snow.SnowDepthColor));
            harmony.Patch(skiff, new HarmonyMethod(typeof(H_SectionLayer_Snow), nameof(Prefix)));
        }

        public static bool Prefix(float snowDepth, ref Color32 __result)
        {
            if (Analyzer.Settings.SnowOptimize)
            {
                __result = new Color32(255, 255, 255, (byte)(byte.MaxValue * snowDepth));
                return false;
            }

            return true;
        }
    }
}