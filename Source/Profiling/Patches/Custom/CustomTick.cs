using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Profiling
{
    [Entry("entry.tick.custom", Category.Tick, "entry.tick.custom.tooltip")]
    internal class CustomProfilersTick
    {
        public static bool Active = false;
        public static HarmonyMethod pre = new HarmonyMethod(typeof(CustomProfilersTick), nameof(Prefix));
        public static HarmonyMethod post = new HarmonyMethod(typeof(CustomProfilersTick), nameof(Postfix));

        public static void PatchMeth(string strde)
        {
            //foreach (string str in Utility.GetSplitString(strde))
            //{
            //    Utility.PatchMethod(str, pre, post);
            //}
        }

        [HarmonyPriority(Priority.Last)]
        public static void Prefix(object __instance, MethodBase __originalMethod, ref Profiler __state)
        {
            if (!Active)
            {
                return;
            }
            string state = string.Empty;
            if (__instance != null)
            {
                state = $"{__instance.GetType().Name}.{__originalMethod.Name}";
            }
            else
            if (__originalMethod.ReflectedType != null)
            {
                state = $"{__originalMethod.ReflectedType.Name}.{__originalMethod.Name}";
            }

            __state = ProfileController.Start(state, null, null, null, null, __originalMethod);
        }

        [HarmonyPriority(Priority.First)]
        public static void Postfix(Profiler __state)
        {
            if (Active)
            {
                __state?.Stop();
            }
        }
    }
}
