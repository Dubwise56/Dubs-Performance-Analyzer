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

        public static void Start(MethodInfo __originalMethod, ref string __state)
        {
            if (Active)
            {
                __state = $"{__originalMethod.DeclaringType} {__originalMethod.Name}" ;
                Analyzer.Start(__state);
            }
        }

        public static void Stop(string __state)
        {
            if (Active) Analyzer.Stop(__state);
        }
    }
}