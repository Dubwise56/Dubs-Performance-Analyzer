using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DubsAnalyzer
{
    [PerformancePatch]
    internal class H_JobDriver_BuildRoof
    {
        public static void PerformancePatch(Harmony harmony)
        {
            var orig = AccessTools.Method(typeof(JobDriver_BuildRoof), nameof(JobDriver_BuildRoof.DoEffect));
            harmony.Patch(orig, null, new HarmonyMethod(typeof(H_JobDriver_BuildRoof), nameof(DoEffectPostfix)));

            orig = AccessTools.Method(typeof(WorkGiver_BuildRoof), nameof(WorkGiver_BuildRoof.PotentialWorkCellsGlobal));
            harmony.Patch(orig, new HarmonyMethod(typeof(H_JobDriver_BuildRoof), nameof(Prefix)));

            var dirt = new HarmonyMethod(typeof(H_JobDriver_BuildRoof), nameof(SetDirty));

            orig = AccessTools.Method(typeof(RoofGrid), nameof(RoofGrid.SetRoof));
            harmony.Patch(orig, dirt);

            orig = AccessTools.Method(typeof(Area), nameof(Area.MarkDirty));
            harmony.Patch(orig, dirt);
        }

        public static Dictionary<int, bool> RoofDirty = new Dictionary<int, bool>();

        public static void SetDirty(object __instance)
        {
            int id = 0;

            if (__instance is RoofGrid rg)
            {
                id =  rg.map.uniqueID;
            }

            if (__instance is Area ar)
            {
                id =   ar.Map.uniqueID;
            }

            try
            {
                RoofDirty[id] = true;
            }
            catch (Exception)
            {
                RoofDirty.Add(id, true);
            }
        }

        public static void CheckDirty(Map map)
        {
            try
            {
                RoofDirty[map.uniqueID] = false;
            }
            catch (Exception)
            {
                RoofDirty.Add(map.uniqueID, false);
            }

            foreach (var buildRoofActiveCell in map.areaManager.BuildRoof.ActiveCells)
            {
                if (!buildRoofActiveCell.Roofed(map))
                {
                    RoofDirty[map.uniqueID] = true;
                    return;
                }
            }
        }

        public static void DoEffectPostfix(JobDriver_BuildRoof __instance)
        {
            if (Analyzer.Settings.OverrideBuildRoof)
            {
                CheckDirty(__instance.Map);
            }
        }

        public static bool Prefix(Pawn pawn, ref IEnumerable<IntVec3> __result)
        {
            if (Analyzer.Settings.OverrideBuildRoof)
            {
                try
                {
                    if (RoofDirty[pawn.Map.uniqueID])
                    {
                        CheckDirty(pawn.Map);
                        return true;
                    }
                }
                catch (Exception)
                {
                    RoofDirty.Add(pawn.Map.uniqueID, true);
                    return true;
                }

                __result = Enumerable.Empty<IntVec3>();
                return false;

            }
            return true;
        }
    }
}