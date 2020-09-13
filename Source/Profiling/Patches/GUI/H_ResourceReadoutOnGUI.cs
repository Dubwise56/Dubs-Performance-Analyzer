using HarmonyLib;
using RimWorld;
using System.Reflection;

namespace Analyzer.Profiling
{
    [Entry("ResourceReadoutOnGUI", Category.GUI)]
    [HarmonyPatch(typeof(ResourceReadout), nameof(ResourceReadout.ResourceReadoutOnGUI))]
    internal class H_ResourceReadoutOnGUI
    {
        public static bool Active = false;

        [HarmonyPriority(Priority.Last)]
        public static void Prefix(MethodBase __originalMethod, ref Profiler __state)
        {
            if (Active)
            {
                __state = ProfileController.Start("ResourceReadoutOnGUI", null, null, null, null, __originalMethod);
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