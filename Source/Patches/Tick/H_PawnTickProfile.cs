using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using Verse;
using Verse.AI;

namespace Analyzer
{
    [Entry("Pawn Tick", Category.Tick)]
    internal static class H_PawnTickProfile
    {
        public static bool Active = false;
        public static void ProfilePatch()
        {
            HarmonyMethod go = new HarmonyMethod(typeof(H_PawnTickProfile), nameof(Start));
            HarmonyMethod biff = new HarmonyMethod(typeof(H_PawnTickProfile), nameof(Stop));

            void slop(Type e, string s)
            {
                Modbase.Harmony.Patch(AccessTools.Method(e, s), go, biff);
            }

            slop(typeof(Pawn), nameof(Pawn.TickRare));
            slop(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.PatherTick));
            slop(typeof(Pawn_StanceTracker), nameof(Pawn_StanceTracker.StanceTrackerTick));
            slop(typeof(VerbTracker), nameof(VerbTracker.VerbsTick));
            slop(typeof(Pawn_NativeVerbs), nameof(Pawn_NativeVerbs.NativeVerbsTick));
            slop(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.JobTrackerTick));
            slop(typeof(Pawn_DrawTracker), nameof(Pawn_DrawTracker.DrawTrackerTick));
            slop(typeof(Pawn_RotationTracker), nameof(Pawn_RotationTracker.RotationTrackerTick));
            slop(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.HealthTick));
            slop(typeof(Pawn_MindState), nameof(Pawn_MindState.MindStateTick));
            slop(typeof(Pawn_CarryTracker), nameof(Pawn_CarryTracker.CarryHandsTick));
            slop(typeof(Pawn_NeedsTracker), nameof(Pawn_NeedsTracker.NeedsTrackerTick));
            slop(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.EquipmentTrackerTick));
            slop(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.ApparelTrackerTick));
            slop(typeof(Pawn_InteractionsTracker), nameof(Pawn_InteractionsTracker.InteractionsTrackerTick));
            slop(typeof(Pawn_CallTracker), nameof(Pawn_CallTracker.CallTrackerTick));
            slop(typeof(Pawn_SkillTracker), nameof(Pawn_SkillTracker.SkillsTick));
            slop(typeof(Pawn_InventoryTracker), nameof(Pawn_InventoryTracker.InventoryTrackerTick));
            slop(typeof(Pawn_DraftController), nameof(Pawn_DraftController.DraftControllerTick));
            slop(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.RelationsTrackerTick));
            slop(typeof(Pawn_GuestTracker), nameof(Pawn_GuestTracker.GuestTrackerTick));
            slop(typeof(Pawn_AgeTracker), nameof(Pawn_AgeTracker.AgeTick));
            slop(typeof(Pawn_RecordsTracker), nameof(Pawn_RecordsTracker.RecordsTick));
        }

        [HarmonyPriority(Priority.Last)]
        public static void Start(MethodInfo __originalMethod, ref Profiler __state)
        {
            if (Active)
            {
                __state = ProfileController.Start(__originalMethod.Name, null, null, null, null, __originalMethod);
            }
        }

        [HarmonyPriority(Priority.Last)]
        public static void Stop(Profiler __state)
        {
            if (Active)
                __state.Stop();
        }
    }
}