using HarmonyLib;
using System;
using System.Reflection;
using Verse;
using Verse.AI;

namespace Analyzer
{
    [Entry("Lord/Duty", Category.Tick)]
    internal class H_GetLord
    {
        public static bool Active = false;

        [HarmonyPriority(Priority.Last)]
        public static void Start(object __instance, MethodBase __originalMethod, ref Profiler __state)
        {
            if (!Active) return;

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
            __state = Analyzer.Start(state, null, null, null, null, __originalMethod);
        }

        [HarmonyPriority(Priority.First)]
        public static void Stop(Profiler __state)
        {
            if (Active)
            {
                __state.Stop();
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
                    Profiler prof = null;
                    try
                    {
                        __state = $"ThinkNode_Priority SubNode [{__instance.subNodes[i].GetType()}]";
                        prof = Analyzer.Start(__state, null, null, null, null, __originalMethod);
                        result = __instance.subNodes[i].TryIssueJobPackage(pawn, jobParams);
                        prof.Stop();
                    }
                    catch (Exception)
                    {
                        prof.Stop();
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

        //public static void Frangle(ref CastPositionRequest newReq)
        //{
        //    if (newReq.maxRangeFromCaster <= 0.01f)
        //    {
        //        // Log.Error($"INFINITE RANGE ON THIS {newReq.caster} {newReq.verb} {newReq.target}");
        //    }
        //}

        public static void ProfilePatch()
        {
            HarmonyMethod go = new HarmonyMethod(typeof(H_GetLord), nameof(Start));
            HarmonyMethod biff = new HarmonyMethod(typeof(H_GetLord), nameof(Stop));

            //void slop(Type e, string s, Type[] types)
            //{
            //    Analyzer.harmony.Patch(AccessTools.Method(e, s, types), go, biff);
            //}
            //   slop(typeof(LordUtility), nameof(LordUtility.GetLord), new Type[] { typeof(Pawn) });
            //  slop(typeof(LordUtility), nameof(LordUtility.GetLord), new Type[] { typeof(Building) });
            //Analyzer.harmony.Patch(AccessTools.Method(typeof(CastPositionFinder), nameof(CastPositionFinder.TryFindCastPosition)), null, new HarmonyMethod(typeof(H_GetLord), nameof(Frangle)));
            Modbase.Harmony.Patch(AccessTools.Method(typeof(ThinkNode_Priority), nameof(ThinkNode_Priority.TryIssueJobPackage)), new HarmonyMethod(typeof(H_GetLord), nameof(Fringe)));
        }
    }
}