using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;

namespace Analyzer.Profiling
{
    [Entry("entry.tick.jobgiver", Category.Tick)]
    public static class H_JobGivers
    {
        public static bool Active = false;

        [Setting("By Pawn")]
        public static bool ByPawn = false;

        public static IEnumerable<MethodInfo> GetPatchMethods()
        {
            foreach (var t in typeof(ThinkNode_JobGiver).AllSubclasses())
            {
                var method = AccessTools.Method(t, "TryGiveJob");
                if(t == method.DeclaringType) yield return method;
            }
                
        }
        public static string GetName(ThinkNode_JobGiver __instance, Pawn pawn)
        {
            var tName = __instance.GetType().Name;
            if (ByPawn && pawn != null) return $"{pawn.KindLabel} - {tName}";
            return tName;
        }
    }
}