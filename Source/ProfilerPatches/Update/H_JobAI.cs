using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;

namespace DubsAnalyzer
{
    [ProfileMode("Job AI", UpdateMode.Update)]
    public class H_JobAI
    {
        public static bool Active = false;

        public static void ProfilePatch()
        {
            var go = new HarmonyMethod(typeof(H_JobAI), nameof(Prefix));
            var biff = new HarmonyMethod(typeof(H_JobAI), nameof(Postfix));

            PatchUtils.PatchType("Pawn_JobTracker", go, biff, false);
        }

        [HarmonyPriority(Priority.Last)]
        public static void Prefix(MethodBase __originalMethod, ref Profiler __state)
        {
            if (Active)
            {
                __state = Analyzer.Start(__originalMethod.Name, null, null, null, null, __originalMethod);
            }
        }

        [HarmonyPriority(Priority.First)]
        public static void Postfix(Profiler __state)
        {
            if (Active)
            {
                __state.Stop();
            }
        }
    }
}
