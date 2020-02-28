using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DubsAnalyzer
{
    [ProfileMode("Custom Tick", UpdateMode.Tick)]
    class CustomProfilersTick
    {
        public static bool Active = false;
        public static void PatchMeth(string str)
        {
            try
            {
                var sav = AccessTools.Method(str);
                if (sav != null)
                {
                    Messages.Message($"Patched {str}", MessageTypeDefOf.TaskCompletion, false);
                    Analyzer.harmony.Patch(sav, new HarmonyMethod(typeof(CustomProfilersTick), nameof(Prefix)),
                        new HarmonyMethod(typeof(CustomProfilersTick), nameof(Postfix)));
                }
                else
                {
                    Messages.Message($"{str} not found", MessageTypeDefOf.NegativeEvent, false);
                }
            }
            catch (Exception e)
            {
                Messages.Message($"catch. {str} failed", MessageTypeDefOf.NegativeEvent, false);
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
                __state = __instance.GetType().Name;
            }
            else if (__originalMethod.ReflectedType != null)
            {
                __state = __originalMethod.ReflectedType.Name;
            }
            else
            {
                __state = __originalMethod.GetType().Name;
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
        public static void PatchMeth(string str)
        {
            try
            {
                var sav = AccessTools.Method(str);
                if (sav != null)
                {
                    Messages.Message($"Patched {str}", MessageTypeDefOf.TaskCompletion, false);
                    Analyzer.harmony.Patch(sav, new HarmonyMethod(typeof(CustomProfilersUpdate), nameof(Prefix)),
                        new HarmonyMethod(typeof(CustomProfilersUpdate), nameof(Postfix)));
                }
                else
                {
                    Messages.Message($"{str} not found", MessageTypeDefOf.NegativeEvent, false);
                }
            }
            catch (Exception e)
            {
                Messages.Message($"catch. {str} failed", MessageTypeDefOf.NegativeEvent, false);
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
                __state = __instance.GetType().Name;
            }
            else if (__originalMethod.ReflectedType != null)
            {
                __state = __originalMethod.ReflectedType.Name;
            }
            else
            {
                __state = __originalMethod.GetType().Name;
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
