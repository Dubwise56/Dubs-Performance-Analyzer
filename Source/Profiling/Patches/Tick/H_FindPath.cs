using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace Analyzer.Profiling
{
    [Entry("entry.tick.pathfinder", Category.Tick)]
    internal class H_FindPath
    {
        public static bool Active = false;

        public static IEnumerable<MethodInfo> GetPatchMethods()
        {
            yield return AccessTools.Method(typeof(Reachability), nameof(Reachability.CanReach),
                new[] {typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms)});
            yield return AccessTools.Method(typeof(PawnUtility), nameof(PawnUtility.ShouldCollideWithPawns));
            yield return AccessTools.Method(typeof(PathFinder), nameof(PathFinder.CalculateDestinationRect));
            yield return AccessTools.Method(typeof(PathFinder), nameof(PathFinder.GetAllowedArea));
            yield return AccessTools.Method(typeof(PathFinder), nameof(PathFinder.DetermineHeuristicStrength));
            yield return AccessTools.Method(typeof(PathFinder), nameof(PathFinder.CalculateAndAddDisallowedCorners));
            yield return AccessTools.Method(typeof(PathFinder), nameof(PathFinder.InitStatusesAndPushStartNode));
        }
    }
}