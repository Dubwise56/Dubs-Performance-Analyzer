using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using Verse;


namespace Analyzer
{
    [Entry("NeedsTracker", Category.Tick)]
    internal static class H_NeedsTrackerTick
    {
        public static bool Active = false;

        [Setting("By pawn")]
        public static bool ByPawn = false;

        public static void ProfilePatch()
        {
            Modbase.Harmony.Patch(AccessTools.Method(typeof(Pawn_NeedsTracker), nameof(Pawn_NeedsTracker.NeedsTrackerTick)), new HarmonyMethod(typeof(H_NeedsTrackerTick), nameof(Detour)));


            HarmonyMethod go = new HarmonyMethod(typeof(H_NeedsTrackerTick), nameof(Start));
            HarmonyMethod biff = new HarmonyMethod(typeof(H_NeedsTrackerTick), nameof(Stop));

            void slop(Type e, string s)
            {
                Modbase.Harmony.Patch(AccessTools.Method(e, s), go, biff);
            }

            slop(typeof(PawnRecentMemory), nameof(PawnRecentMemory.RecentMemoryInterval));
            slop(typeof(ThoughtHandler), nameof(ThoughtHandler.ThoughtInterval));
            slop(typeof(PawnObserver), nameof(PawnObserver.ObserverInterval));
        }

        [HarmonyPriority(Priority.Last)]
        public static void Start(MethodInfo __originalMethod, ref Profiler __state)
        {
            if (Active)
            {
                __state = Analyzer.Start(__originalMethod.Name, null, null, null, null, __originalMethod);
            }
        }

        [HarmonyPriority(Priority.First)]
        public static void Stop(Profiler __state)
        {
            if (Active)
            {
                __state.Stop();
            }
        }

        public static bool Detour(MethodInfo __originalMethod, Pawn_NeedsTracker __instance, ref string __state)
        {
            if (Active)
            {
                if (__instance.pawn.IsHashIntervalTick(150))
                {
                    for (int i = 0; i < __instance.needs.Count; i++)
                    {
                        if (ByPawn)
                        {
                            __state = $"{__instance.needs[i].GetType().Name} {__instance.needs[i].pawn}";
                        }
                        else
                        {
                            __state = $"{__instance.needs[i].GetType().Name}";
                        }

                        Profiler prof = Analyzer.Start(__state, null, null, null, null, __originalMethod);
                        __instance.needs[i].NeedInterval();
                        prof.Stop();
                    }
                }
                return false;
            }
            return true;
        }
    }
}