using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;


namespace Analyzer.Profiling
{
    [Entry("NeedsTracker", Category.Tick)]
    internal static class H_NeedsTrackerTick
    {
        public static bool Active = false;

        [Setting("By pawn")]
        public static bool ByPawn = false;

        public static IEnumerable<MethodInfo> GetPatchMethods()
        {
            return typeof(Need).AllSubclasses().Select(n => n.GetMethod("NeedInterval"));
        }

        public static string GetName(Need __instance)
        {
            if (ByPawn)
            {
                return __instance.GetType().Name + " " + __instance.pawn.Name;
            }
            else
            {
                return __instance.GetType().Name;
            }
        }

        //slop(typeof(PawnRecentMemory), nameof(PawnRecentMemory.RecentMemoryInterval));
        //slop(typeof(ThoughtHandler), nameof(ThoughtHandler.ThoughtInterval));
        //slop(typeof(PawnObserver), nameof(PawnObserver.ObserverInterval));
    }
}