using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace DubsAnalyzer
{
    //[HarmonyPatch(typeof(JobGiver_AITrashBuildingsDistant), nameof(JobGiver_AITrashBuildingsDistant.TryGiveJob))]
    //internal class H_JobGiver_AITrashBuildingsDistant
    //{
    //    public static bool Prefix(JobGiver_AITrashBuildingsDistant __instance, Pawn pawn, ref Job __result)
    //    {
    //        if (Analyzer.running && Analyzer.UpdateMode == UpdateMode.Tick)
    //        {
    //            if (Analyzer.loggingMode == LoggingMode.MiscTick)
    //            {
    //                __result = null;
    //                var __state = string.Intern($"{__instance} from {pawn}");
    //                Analyzer.Start(__state);
    //                List<Building> allBuildingsColonist = pawn.Map.listerBuildings.allBuildingsColonist;
    //                if (allBuildingsColonist.Count == 0)
    //                {
    //                    return false;
    //                }
    //                for (int i = 0; i < 75; i++)
    //                {
    //                    Building building = allBuildingsColonist.RandomElement<Building>();
    //                    if (TrashUtility.ShouldTrashBuilding(pawn, building, __instance.attackAllInert))
    //                    {
    //                        Job job = TrashUtility.TrashJob(pawn, building, __instance.attackAllInert);
    //                        if (job != null)
    //                        {
    //                            __result = job;
    //                            return false;
    //                        }
    //                    }
    //                }
    //                Analyzer.Stop(__state);
    //                return false;
    //            }
    //        }
    //        return true;
    //    }

    //}
}