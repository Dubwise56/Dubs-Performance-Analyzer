using HarmonyLib;
using System.Reflection;

namespace Analyzer
{
    [Entry("Job AI", Category.Update)]
    public class H_JobAI
    {
        public static bool Active = false;

        public static void ProfilePatch()
        {
            HarmonyMethod go = new HarmonyMethod(typeof(H_JobAI), nameof(Prefix));
            HarmonyMethod biff = new HarmonyMethod(typeof(H_JobAI), nameof(Postfix));

            Utility.PatchType("Pawn_JobTracker", go, biff, false);
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
