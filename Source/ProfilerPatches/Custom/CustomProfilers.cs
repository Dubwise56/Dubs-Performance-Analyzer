using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DubsAnalyzer
{
    [ProfileMode("Custom Tick", UpdateMode.Tick)]
    class CustomProfilersTick
    {
        public static bool Active = false;
        public static HarmonyMethod pre = new HarmonyMethod(typeof(CustomProfilersTick), nameof(Prefix));
        public static HarmonyMethod post = new HarmonyMethod(typeof(CustomProfilersTick), nameof(Postfix));

        public static void PatchMeth(string strde)
        {
            foreach (var str in PatchUtils.GetSplitString(strde))
            {
                PatchUtils.PatchMethod(str, pre, post);
            }
        }

        [HarmonyPriority(Priority.Last)]
        public static void Prefix(object __instance, MethodBase __originalMethod, ref Profiler __state)
        {
            if (!Active)
            {
                return;
            }
            var state = string.Empty;
            if (__instance != null)
            {
                state = $"{__instance.GetType().Name}.{__originalMethod.Name}";
            }
            else
            if (__originalMethod.ReflectedType != null)
            {
                state = $"{__originalMethod.ReflectedType.Name}.{__originalMethod.Name}";
            }

            __state = Analyzer.Start(state, null, null, null, null, __originalMethod);
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

    [ProfileMode("Custom Update", UpdateMode.Update)]
    class CustomProfilersUpdate
    {
        public static bool Active = false;
        public static HarmonyMethod pre = new HarmonyMethod(typeof(CustomProfilersUpdate), nameof(Prefix));
        public static HarmonyMethod post = new HarmonyMethod(typeof(CustomProfilersUpdate), nameof(Postfix));
        public static void PatchMeth(string strde)
        {
            foreach (var str in PatchUtils.GetSplitString(strde))
            {
                PatchUtils.PatchMethod(str, pre, post);
            }
        }

        public static void PatchType(string strde)
        {
            foreach (var str in PatchUtils.GetSplitString(strde))
            {
                PatchUtils.PatchType(str, pre, post);
            }
        }

        public static void PatchAssembly(string strde)
        {
            PatchUtils.PatchAssembly(strde, pre, post);
        }

        [HarmonyPriority(Priority.Last)]
        public static void Prefix(object __instance, MethodBase __originalMethod, ref Profiler __state)
        {

            if (!Active)
            {
                return;
            }
            var state = string.Empty;
            if (__instance != null)
            {
                state = $"{__instance.GetType().Name}.{__originalMethod.Name}";
            }
            else
            if (__originalMethod.ReflectedType != null)
            {
                state = $"{__originalMethod.ReflectedType.Name}.{__originalMethod.Name}";
            }

            __state = Analyzer.Start(state, null, null, null, null, __originalMethod);
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

    [ProfileMode("Custom Harmony", UpdateMode.Update)]
    class CustomProfilersHarmony
    {
        public static bool Active = false;
        public static HarmonyMethod pre = new HarmonyMethod(typeof(CustomProfilersHarmony), nameof(Prefix));
        public static HarmonyMethod post = new HarmonyMethod(typeof(CustomProfilersHarmony), nameof(Postfix));
        public static void PatchMeth(string strde)
        {
            foreach (var str in PatchUtils.GetSplitString(strde))
            {
                PatchUtils.PatchMethodPatches(str, pre, post);
            }
        }
        public static void PatchType(string strde)
        {
            foreach (var str in PatchUtils.GetSplitString(strde))
            {
                PatchUtils.PatchTypePatches(str, pre, post);
            }
        }


        [HarmonyPriority(Priority.Last)]
        public static void Prefix(object __instance, MethodBase __originalMethod, ref Profiler __state)
        {

            if (!Active)
            {
                return;
            }
            var state = string.Empty;
            if (__instance != null)
            {
                state = $"{__instance.GetType().Name}.{__originalMethod.Name}";
            }
            else
            if (__originalMethod.ReflectedType != null)
            {
                state = $"{__originalMethod.ReflectedType.Name}.{__originalMethod.Name}";
            }

            __state = Analyzer.Start(state, null, null, null, null, __originalMethod);
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
