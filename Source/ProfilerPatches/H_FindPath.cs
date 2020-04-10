using System;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace DubsAnalyzer
{
    [ProfileMode("PathFinder", UpdateMode.Tick)]
    [HarmonyPatch(typeof(PathFinder), nameof(PathFinder.FindPath), typeof(IntVec3), typeof(LocalTargetInfo), typeof(TraverseParms), typeof(PathEndMode))]
    internal class H_FindPath
    {
        public static bool Active = false;

        public static bool pathing;

        public static int NodeIndex = 0;

        public static void ProfilePatch()
        {
            var go = new HarmonyMethod(typeof(H_FindPath), nameof(Start));
            var biff = new HarmonyMethod(typeof(H_FindPath), nameof(Stop));

            void slop(Type e, string s)
            {
                Analyzer.harmony.Patch(AccessTools.Method(e, s), go, biff);
            }

            var mad = AccessTools.Method(typeof(Reachability), nameof(Reachability.CanReach),
                new[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms) });
            Analyzer.harmony.Patch(mad, go, biff);

           // slop(typeof(PathFinder), nameof(PathFinder.CalculateDestinationRect));
           slop(typeof(PathFinder), nameof(PathFinder.GetAllowedArea));
           slop(typeof(PawnUtility), nameof(PawnUtility.ShouldCollideWithPawns));
            slop(typeof(PathFinder), nameof(PathFinder.DetermineHeuristicStrength));
            slop(typeof(PathFinder), nameof(PathFinder.CalculateAndAddDisallowedCorners));
            slop(typeof(PathFinder), nameof(PathFinder.InitStatusesAndPushStartNode));
        }

        public static void Start(MethodBase __originalMethod, ref string __state)
        {
            if (Active && pathing)
            {
                __state = __originalMethod.Name;
                Analyzer.Start(__state);
            }
        }

        public static void Stop(string __state)
        {
            if (Active && pathing)
            {
                Analyzer.Stop(__state);
            }
        }

        public static void Prefix(ref string __state)
        {
            if (Active)
            {
                __state = "PathFinder.FindPath";
                Analyzer.Start(__state);
                pathing = true;
            }
        }

        public static void Postfix(string __state)
        {
            if (Active)
            {
                pathing = false;
                Analyzer.Stop(__state);
            }
        }
    }
}