using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DubsAnalyzer
{
}

namespace DubsAnalyzer
{
    [ProfileMode("NeedsTracker", UpdateMode.Tick)]
    internal static class H_NeedsTrackerTick
    {
        public static bool Active = false;

        [Setting("By pawn")]
        public static bool ByPawn=false;

        public static void ProfilePatch()
        {
            Analyzer.harmony.Patch(AccessTools.Method(typeof(Pawn_NeedsTracker), nameof(Pawn_NeedsTracker.NeedsTrackerTick)), new HarmonyMethod(typeof(H_NeedsTrackerTick), nameof(Detour)));


            var go = new HarmonyMethod(typeof(H_NeedsTrackerTick), nameof(Start));
            var biff = new HarmonyMethod(typeof(H_NeedsTrackerTick), nameof(Stop));

            void slop(Type e, string s)
            {
                Analyzer.harmony.Patch(AccessTools.Method(e, s), go, biff);
            }

            slop(typeof(PawnRecentMemory), nameof(PawnRecentMemory.RecentMemoryInterval));
            slop(typeof(ThoughtHandler), nameof(ThoughtHandler.ThoughtInterval));
            slop(typeof(PawnObserver), nameof(PawnObserver.ObserverInterval));
        }

        public static void Start(MethodInfo __originalMethod, ref string __state)
        {
            if (Active)
            {
                __state = __originalMethod.Name;
                Analyzer.Start(__state);
            }
        }

        public static void Stop(string __state)
        {
            if (Active) Analyzer.Stop(__state);
        }

        public static bool Detour(Pawn_NeedsTracker __instance, ref string __state)
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

                        Analyzer.Start(__state);
                        __instance.needs[i].NeedInterval();
                        Analyzer.Stop(__state);
                    }
                }
                return false;
            }
            return true;
        }
    }
}