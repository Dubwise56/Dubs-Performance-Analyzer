using HarmonyLib;
using RimWorld;
using System.Reflection;

namespace Analyzer
{

    [Entry("ColonistBarOnGUI", Category.GUI)]
    [HarmonyPatch(typeof(ColonistBar), nameof(ColonistBar.ColonistBarOnGUI))]
    internal class H_ColonistBarOnGUI
    {
        public static bool Active = false;

        [HarmonyPriority(Priority.Last)]
        public static void Prefix(MethodBase __originalMethod, ref Profiler __state)
        {
            if (Active)
            {
                __state = ProfileController.Start("ColonistBarOnGUI", null, null, null, null, __originalMethod);
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