using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace Analyzer.Profiling.Patches.Tick
{
    [Entry("entry.tick.jobdriver", Category.Tick)]
    class H_JobDriver
    {
        public static bool Active = false;

        [Setting("By Pawn")]
        public static bool ByPawn = false;

        public static IEnumerable<MethodInfo> GetPatchMethods()
        {
            foreach (var t in typeof(JobDriver).AllSubclasses())
            {
                var method = AccessTools.Method(t, "DriverTick");
                if(t == method.DeclaringType) yield return method;
            }

            yield return AccessTools.Method(typeof(JobDriver), "DriverTick");
        }

        public static string GetName(JobDriver __instance)
        {
            var str = $"{__instance.GetType().Name}";
            return ByPawn
                ? $"{__instance.pawn.KindLabel} - {str}"
                : str;
        }


        
    }
}
