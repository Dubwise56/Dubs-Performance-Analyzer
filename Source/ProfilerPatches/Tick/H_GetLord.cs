using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace DubsAnalyzer
{
    [ProfileMode("Lord/Duty", UpdateMode.Tick)]
    internal class H_GetLord
    {
        public static bool Active = false;

        [HarmonyPriority(Priority.Last)]
        public static void Start(object __instance, MethodBase __originalMethod, ref string __state)
        {
            if (!Active || !AnalyzerState.CurrentlyRunning) return;
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
            Analyzer.Start(__state, null, null, null, null, __originalMethod as MethodInfo);
        }

        [HarmonyPriority(Priority.First)]
        public static void Stop(string __state)
        {
            if (Active && !string.IsNullOrEmpty(__state))
            {
                Analyzer.Stop(__state);
            }
        }

        public static bool Fringe(MethodBase __originalMethod, ThinkNode_Priority __instance, Pawn pawn, JobIssueParams jobParams, ref string __state, ref ThinkResult __result)
        {
            if (Active)
            {
                int count = __instance.subNodes.Count;
                for (int i = 0; i < count; i++)
                {
                    ThinkResult result = ThinkResult.NoJob;
                    try
                    {
                        __state = $"ThinkNode_Priority SubNode [{__instance.subNodes[i].GetType()}]";
                        Analyzer.Start(__state, null, null, null, null, __originalMethod as MethodInfo);
                        result = __instance.subNodes[i].TryIssueJobPackage(pawn, jobParams);
                        Analyzer.Stop(__state);
                    }
                    catch (Exception)
                    {
                        Analyzer.Stop(__state);
                    }
                    if (result.IsValid)
                    {
                        __result = result;
                        return false;
                    }
                }
                __result = ThinkResult.NoJob;

                return false;
            }
            return true;
        }

        public static void Frangle(ref CastPositionRequest newReq)
        {
            if (newReq.maxRangeFromCaster <= 0.01f)
            {
                // Log.Error($"INFINITE RANGE ON THIS {newReq.caster} {newReq.verb} {newReq.target}");
            }
        }

        public static void ProfilePatch()
        {
            var go = new HarmonyMethod(typeof(H_GetLord), nameof(Start));
            var biff = new HarmonyMethod(typeof(H_GetLord), nameof(Stop));

            //void slop(Type e, string s, Type[] types)
            //{
            //    Analyzer.harmony.Patch(AccessTools.Method(e, s, types), go, biff);
            //}
            //   slop(typeof(LordUtility), nameof(LordUtility.GetLord), new Type[] { typeof(Pawn) });
            //  slop(typeof(LordUtility), nameof(LordUtility.GetLord), new Type[] { typeof(Building) });
            Analyzer.harmony.Patch(AccessTools.Method(typeof(CastPositionFinder), nameof(CastPositionFinder.TryFindCastPosition)), null, new HarmonyMethod(typeof(H_GetLord), nameof(Frangle)));
            Analyzer.harmony.Patch(AccessTools.Method(typeof(ThinkNode_Priority), nameof(ThinkNode_Priority.TryIssueJobPackage)), new HarmonyMethod(typeof(H_GetLord), nameof(Fringe)));
        }
    }
}