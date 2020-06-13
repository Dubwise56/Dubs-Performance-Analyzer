using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.Sound;
using Verse.Steam;

namespace DubsAnalyzer
{
    [ProfileMode("Root", UpdateMode.Update)]
    internal static class H_Root
    {
        public static bool Active = false;

        public static void ProfilePatch()
        {

            var go = new HarmonyMethod(typeof(H_Root), nameof(Start));
            var biff = new HarmonyMethod(typeof(H_Root), nameof(Stop));

            void slop(Type e, string s)
            {
                Analyzer.harmony.Patch(AccessTools.Method(e, s), go, biff);
            }

            slop(typeof(ResolutionUtility), nameof(ResolutionUtility.Update));
            slop(typeof(RealTime), nameof(RealTime.Update));
            slop(typeof(LongEventHandler), nameof(LongEventHandler.LongEventsUpdate));
            slop(typeof(Rand), nameof(Rand.EnsureStateStackEmpty));
            slop(typeof(Widgets), nameof(Widgets.EnsureMousePositionStackEmpty));
            slop(typeof(SteamManager), nameof(SteamManager.Update));
            slop(typeof(PortraitsCache), nameof(PortraitsCache.PortraitsCacheUpdate));
            slop(typeof(AttackTargetsCache), nameof(AttackTargetsCache.AttackTargetsCacheStaticUpdate));
            slop(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.PawnMeleeVerbsStaticUpdate));
            slop(typeof(Storyteller), nameof(Storyteller.StorytellerStaticUpdate));
            slop(typeof(CaravanInventoryUtility), nameof(CaravanInventoryUtility.CaravanInventoryUtilityStaticUpdate));
            slop(typeof(UIRoot), nameof(UIRoot.UIRootUpdate));
            slop(typeof(SoundRoot), nameof(SoundRoot.Update));
        }

        [HarmonyPriority(Priority.Last)]
        public static void Start(MethodInfo __originalMethod, ref Profiler __state)
        {
            if (Active)
            {
                __state = Analyzer.Start($"{__originalMethod.DeclaringType} - {__originalMethod.Name}", null, null, null, null, __originalMethod);
            }
        }

        [HarmonyPriority(Priority.First)]
        public static void Stop(Profiler __state)
        {
            if (Active) 
                __state.Stop();
        }
    }
}