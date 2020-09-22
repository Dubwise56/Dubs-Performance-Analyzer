using HarmonyLib;
using System.Reflection;

namespace Analyzer.Profiling
{
    [Entry("entry.update.custom", Category.Update, "entry.update.custom.tooltip")]
    internal class CustomProfilersUpdate
    {
        public static bool Active = false;
        public static HarmonyMethod pre = new HarmonyMethod(typeof(CustomProfilersUpdate), nameof(Prefix));
        public static HarmonyMethod post = new HarmonyMethod(typeof(CustomProfilersUpdate), nameof(Postfix));

        public static void PatchMeth(string strde)
        {
            //foreach (string str in Utility.GetSplitString(strde))
            //{
            //    Utility.PatchMethod(str, pre, post);
            //}
        }

        public static void PatchType(string strde)
        {
            //foreach (string str in Utility.GetSplitString(strde))
            //{
            //    Utility.PatchType(str, pre, post);
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
