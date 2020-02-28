using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace DubsAnalyzer
{
    // [StaticConstructorOnStartup]
    //  [HarmonyPatch(typeof(WorkGiver_Warden), nameof(WorkGiver_Warden.PotentialWorkThingRequest), MethodType.Getter)]
    internal class H_WardenRequest
    {
        public static List<ThingDef> doables;

        private static Type BasicReleasePrisoner;
        private static Type ReleasePrisoner;
        private static Type DoExecution;

        public static void Swapclasses()
        {
            if (Analyzer.Settings.HumanoidOnlyWarden)
            {
                DefDatabase<WorkGiverDef>.GetNamed("BasicReleasePrisoner").giverClass = typeof(WorkGiver_Warden_ReleasePrisonerFixed);
                DefDatabase<WorkGiverDef>.GetNamed("ReleasePrisoner").giverClass = typeof(WorkGiver_Warden_ReleasePrisonerFixed);
                DefDatabase<WorkGiverDef>.GetNamed("DoExecution").giverClass = typeof(WorkGiver_Warden_DoExecutionFixed);
            }
            else
            {
                DefDatabase<WorkGiverDef>.GetNamed("BasicReleasePrisoner").giverClass = BasicReleasePrisoner;
                DefDatabase<WorkGiverDef>.GetNamed("ReleasePrisoner").giverClass = ReleasePrisoner;
                DefDatabase<WorkGiverDef>.GetNamed("DoExecution").giverClass = DoExecution;
            }
        }

        public static void PatchMe()
        {
            doables = DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.race != null && x.race.Humanlike).ToList();

            BasicReleasePrisoner = DefDatabase<WorkGiverDef>.GetNamed("BasicReleasePrisoner").giverClass;
            ReleasePrisoner = DefDatabase<WorkGiverDef>.GetNamed("ReleasePrisoner").giverClass;
            DoExecution = DefDatabase<WorkGiverDef>.GetNamed("DoExecution").giverClass;
            Swapclasses();
        }


        //public static void Postfix(ref ThingRequest __result)
        //{
        //    if (Analyzer.Settings.HumanOnlyWarden)
        //    {
        //        __result = ThingRequest.ForDef(ThingDefOf.Human);
        //    }
        //}
    }


    //was 4.2 now 1.5
    public class WorkGiver_Warden_ReleasePrisonerFixed : WorkGiver_Warden_ReleasePrisoner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Undefined);

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            var dc = H_WardenRequest.doables.Count;
            for (var i = 0; i < dc; i++)
            {
                var list = pawn.Map.listerThings.listsByDef[H_WardenRequest.doables[i]];
                var z = list.Count;
                for (var index = 0; index < z; index++)
                {
                    yield return list[index];
                }
            }
        }
    }
    //was 2.6 now 2.2
    public class WorkGiver_Warden_DoExecutionFixed : WorkGiver_Warden_DoExecution
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Undefined);

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            var dc = H_WardenRequest.doables.Count;
            for (var i = 0; i < dc; i++)
            {
                var list = pawn.Map.listerThings.listsByDef[H_WardenRequest.doables[i]];
                var z = list.Count;
                for (var index = 0; index < z; index++)
                {
                    yield return list[index];
                }
            }
        }
    }
}