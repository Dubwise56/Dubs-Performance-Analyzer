using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DubsAnalyzer
{

  //  [HarmonyPatch(typeof(WorkGiver_DoBill), nameof(WorkGiver_DoBill.PotentialWorkThingsGlobal))]
    static class H_WorkGiver_DoBillPotentialWorkThingsGlobal
    {
        public static void PatchMe()
        {
           // var jiff = AccessTools.Method(typeof(WorkGiver_DoBill), nameof(WorkGiver_DoBill.PotentialWorkThingRequest), new[] { typeof(Pawn) });
          //  var pre = new HarmonyMethod(typeof(H_ComfortableTemperatureRange), nameof(Prefix));
          //  Analyzer.harmony.Patch(jiff, pre);
        }

        public static void Postfix(WorkGiver_DoBill __instance, Pawn pawn, ref ThingRequest __result)
        {

          //  BillUtility.GlobalBills()

            if (__instance.def.billGiversAllAnimals)
            {
                __result = ThingRequest.ForGroup(ThingRequestGroup.Pawn);
            }
            if (__instance.def.billGiversAllHumanlikes)
            {
                __result = ThingRequest.ForGroup(ThingRequestGroup.Pawn);
            }
            if (__instance.def.billGiversAllMechanoids)
            {
                __result = ThingRequest.ForGroup(ThingRequestGroup.Pawn);
            }
            if (__instance.def.billGiversAllAnimalsCorpses)
            {
                __result = ThingRequest.ForGroup(ThingRequestGroup.Corpse);
            }
            if (__instance.def.billGiversAllHumanlikesCorpses)
            {
                __result = ThingRequest.ForGroup(ThingRequestGroup.Corpse);
            }
            if (__instance.def.billGiversAllMechanoidsCorpses)
            {
                __result = ThingRequest.ForGroup(ThingRequestGroup.Corpse);
            }
        }
    }

    class WorkGiver_DoBillFixed : WorkGiver_DoBill
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            var flange = 0;
            return base.PotentialWorkThingsGlobal(pawn);
        }
    }

    //[HarmonyPatch(typeof(WorkGiver_Scanner), nameof(WorkGiver_Scanner.PotentialWorkThingRequest))]
    //static class H_WorkGiver_DoBill
    //{
    //    public static void PatchMe()
    //    {
    //        //var jiff = AccessTools.Method(typeof(WorkGiver_DoBill), nameof(WorkGiver_DoBill.PotentialWorkThingRequest), new[] { typeof(Pawn) });
    //        //var pre = new HarmonyMethod(typeof(H_ComfortableTemperatureRange), nameof(Prefix));
    //        //Analyzer.harmony.Patch(jiff, pre);
    //    }

    //    public static void Postfix(WorkGiver_Scanner __instance, IEnumerable<Thing> __result)
    //    {
    //        if (__instance is WorkGiver_DoBill Clinton)
    //        {

    //        }
    //       // __result = null;
    //    }
    //}

    //[HarmonyPatch(typeof(WorkGiver_Scanner), nameof(WorkGiver_Scanner.PotentialWorkThingRequest))]
    //static class H_WorkGiver_DoBill
    //{
    //    public static void PatchMe()
    //    {
    //        //var jiff = AccessTools.Method(typeof(WorkGiver_DoBill), nameof(WorkGiver_DoBill.PotentialWorkThingRequest), new[] { typeof(Pawn) });
    //        //var pre = new HarmonyMethod(typeof(H_ComfortableTemperatureRange), nameof(Prefix));
    //        //Analyzer.harmony.Patch(jiff, pre);
    //    }

    //    public static void Postfix(WorkGiver_Scanner __instance, IEnumerable<Thing> __result)
    //    {
    //        if (__instance is WorkGiver_DoBill Clinton)
    //        {

    //        }
    //        // __result = null;
    //    }
    //}
}