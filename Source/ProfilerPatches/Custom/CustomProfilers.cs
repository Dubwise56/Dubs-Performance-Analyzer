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

        public static void Prefix(object __instance, MethodBase __originalMethod, ref string __state)
        {
            if (!Active)
            {
                return;
            }
            __state = string.Empty;
            if (__instance != null)
            {
                __state = $"{__instance.GetType().Name}.{__originalMethod.Name}";
            }
            else
            if (__originalMethod.ReflectedType != null)
            {
                __state = $"{__originalMethod.ReflectedType.Name}.{__originalMethod.Name}";
            }

            Analyzer.Start(__state);
        }

        public static void Postfix(string __state)
        {
            if (Active)
            {
                Analyzer.Stop(__state);
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

        public static void Prefix(object __instance, MethodBase __originalMethod, ref string __state)
        {
            
            if (!Active)
            {
                return;
            }
            __state = string.Empty;
            if (__instance != null)
            {
                __state = $"{__instance.GetType().Name}.{__originalMethod.Name}";
            }
            else
            if (__originalMethod.ReflectedType != null)
            {
                __state = $"{__originalMethod.ReflectedType.Name}.{__originalMethod.Name}";
            }

            Analyzer.Start(__state);
        }

        public static void Postfix(string __state)
        {
            if (Active)
            {
                Analyzer.Stop(__state);
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

        public static void Prefix(object __instance, MethodBase __originalMethod, ref string __state)
        {

            if (!Active)
            {
                return;
            }
            __state = string.Empty;
            if (__instance != null)
            {
                __state = $"{__instance.GetType().Name}.{__originalMethod.Name}";
            }
            else
            if (__originalMethod.ReflectedType != null)
            {
                __state = $"{__originalMethod.ReflectedType.Name}.{__originalMethod.Name}";
            }

            Analyzer.Start(__state);
        }

        public static void Postfix(string __state)
        {
            if (Active)
            {
                Analyzer.Stop(__state);
            }
        }
    }

}
