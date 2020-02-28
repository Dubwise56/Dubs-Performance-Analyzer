using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace DubsAnalyzer
{
    //[HarmonyPatch(typeof(WorkGiver_Warden), nameof(WorkGiver_Warden.ShouldTakeCareOfPrisoner))]
    //internal class H_ShouldTakeCareOfPrisoner
    //{
    //    public static void Prefix(Pawn warden)
    //    {
    //        if (Analyzer.running && Analyzer.UpdateMode == UpdateMode.Tick)
    //        {
    //            if (Analyzer.loggingMode == LoggingMode.MiscTick)
    //            {
    //                Analyzer.Start("ShouldTakeCareOfPrisoner " + warden.Label);
    //            }
    //        }
    //    }

    //    public static void Postfix(Pawn warden)
    //    {
    //        if (Analyzer.running)
    //        {
    //            if (Analyzer.UpdateMode == UpdateMode.Tick && Analyzer.loggingMode == LoggingMode.MiscTick)
    //            {
    //                Analyzer.Stop("ShouldTakeCareOfPrisoner " + warden.Label);
    //            }
    //        }
    //    }
    //}


    //[HarmonyPatch(typeof(TransitionAction_CheckForJobOverride), nameof(TransitionAction_CheckForJobOverride.DoAction))]
    //internal class H_CheckSignal
    //{
    //    public static bool Prefix(Transition trans)
    //    {
    //        if (!Analyzer.running || Analyzer.UpdateMode != UpdateMode.Tick)
    //        {
    //            return true;
    //        }

    //        if (Analyzer.loggingMode != LoggingMode.MiscTick)
    //        {
    //            return true;
    //        }

    //        Analyzer.Start("TransitionAction_CheckForJobOverride");

    //        var ownedPawns = trans.target.lord.ownedPawns;
    //        for (var i = 0; i < ownedPawns.Count; i++)
    //        {
    //            if (ownedPawns[i].CurJob != null)
    //            {
    //                Analyzer.Start(ownedPawns[i].Label);
    //                ownedPawns[i].jobs.CheckForJobOverride();
    //                Analyzer.Stop(ownedPawns[i].Label);
    //            }
    //        }

    //        Analyzer.Stop("TransitionAction_CheckForJobOverride");

    //        return false;
    //    }
    //}
    //[HarmonyPatch(typeof(ThinkNode_Tagger), nameof(ThinkNode_Tagger.TryIssueJobPackage))]
    //internal class H_ThinkNode_Tagger
    //{
    //    public static void Prefix(ThinkNode_Subtree __instance)
    //    {
    //        if (!Analyzer.running || Analyzer.UpdateMode != UpdateMode.Tick)
    //        {
    //            return;
    //        }
    //        if (Analyzer.loggingMode != LoggingMode.MiscTick)
    //        {
    //            return;
    //        }

    //        var jeb = __instance.ToString();
    //        Analyzer.Start(jeb);
    //    }

    //    public static void Postfix(string __state)
    //    {
    //        if (__state != string.Empty) Analyzer.Stop(__state);
    //    }
    //}

    //[HarmonyPatch(typeof(ThinkNode_ConditionalHasVoluntarilyJoinableLord), nameof(ThinkNode_ConditionalHasVoluntarilyJoinableLord.TryIssueJobPackage))]
    //internal class H_ThinkNode_ConditionalHasVoluntarilyJoinableLord
    //{
    //    public static bool Prefix(ThinkNode_Subtree __instance, Pawn pawn, JobIssueParams jobParams,
    //        ref ThinkResult __result)
    //    {
    //        if (!Analyzer.running || Analyzer.UpdateMode != UpdateMode.Tick)
    //        {
    //            return true;
    //        }
    //        if (Analyzer.loggingMode != LoggingMode.MiscTick)
    //        {
    //            return true;
    //        }

    //        var jeb = __instance.subtreeNode.ToString();
    //        Analyzer.Start(jeb);
    //        __result = __instance.subtreeNode.TryIssueJobPackage(pawn, jobParams);
    //        Analyzer.Stop(jeb);

    //        return false;
    //    }
    //}

    //[HarmonyPatch(typeof(ThinkNode_Subtree), nameof(ThinkNode_Subtree.TryIssueJobPackage))]
    //internal class H_ThinkNode_Subtree
    //{
    //    public static bool Prefix(ThinkNode_Subtree __instance, Pawn pawn, JobIssueParams jobParams,
    //        ref ThinkResult __result)
    //    {
    //        if (!Analyzer.running || Analyzer.UpdateMode != UpdateMode.Tick)
    //        {
    //            return true;
    //        }
    //        if (Analyzer.loggingMode != LoggingMode.MiscTick)
    //        {
    //            return true;
    //        }

    //        var jeb = __instance.subtreeNode.ToString();
    //        Analyzer.Start(jeb);
    //        __result = __instance.subtreeNode.TryIssueJobPackage(pawn, jobParams);
    //        Analyzer.Stop(jeb);

    //        return false;
    //    }
    //}

    //[HarmonyPatch(typeof(ThinkNode_FilterPriority), nameof(ThinkNode_FilterPriority.TryIssueJobPackage))]
    //internal class H_ThinkNode_FilterPriority
    //{
    //    public static bool Prefix(ThinkNode_FilterPriority __instance, Pawn pawn, JobIssueParams jobParams,
    //        ref ThinkResult __result)
    //    {
    //        if (!Analyzer.running || Analyzer.UpdateMode != UpdateMode.Tick)
    //        {
    //            return true;
    //        }
    //        if (Analyzer.loggingMode != LoggingMode.MiscTick)
    //        {
    //            return true;
    //        }

    //        int count = __instance.subNodes.Count;
    //        for (int i = 0; i < count; i++)
    //        {
    //            if (__instance.subNodes[i].GetPriority(pawn) > __instance.minPriority)
    //            {
    //                var jeb = $"{__instance.subNodes[i]}";
    //                Analyzer.Start(jeb);
    //                ThinkResult result = __instance.subNodes[i].TryIssueJobPackage(pawn, jobParams);
    //                result = __instance.subNodes[i].TryIssueJobPackage(pawn, jobParams);
    //                Analyzer.Stop(jeb);
    //                if (result.IsValid)
    //                {
    //                    __result = result;
    //                    return false;
    //                }
    //            }
    //        }
    //        __result = ThinkResult.NoJob;
    //        return false;
    //    }
    //}


    //[HarmonyPatch(typeof(ThinkNode_Priority), nameof(ThinkNode_Priority.TryIssueJobPackage))]
    //internal class H_ThinkNode_Priority
    //{
    //    public static bool Prefix(ThinkNode_Priority __instance, Pawn pawn, JobIssueParams jobParams,
    //        ref ThinkResult __result)
    //    {
    //        if (!Analyzer.running || Analyzer.UpdateMode != UpdateMode.Tick)
    //        {
    //            return true;
    //        }
    //        if (Analyzer.loggingMode != LoggingMode.MiscTick)
    //        {
    //            return true;
    //        }


    //        var count = __instance.subNodes.Count;
    //        for (var i = 0; i < count; i++)
    //        {
    //            var result = ThinkResult.NoJob;
    //            try
    //            {
    //                var jeb = $"{__instance.subNodes[i]}";
    //                Analyzer.Start(jeb);
    //                result = __instance.subNodes[i].TryIssueJobPackage(pawn, jobParams);
    //                Analyzer.Stop(jeb);
    //            }
    //            catch (Exception ex)
    //            {
    //                Log.Error(string.Concat("Exception in ", __instance.GetType(), " TryIssueJobPackage: ",
    //                    ex.ToString()));
    //            }

    //            if (result.IsValid)
    //            {
    //                __result = result;

    //                return false;
    //            }
    //        }

    //        __result = ThinkResult.NoJob;
    //        return false;
    //    }
    //}


    //[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.DetermineNextJob))]
    //internal class H_DetermineNextJob
    //{
    //    public static bool Prefix(Pawn_JobTracker __instance, ref ThinkTreeDef thinkTree, ref ThinkResult __result)
    //    {
    //        if (!Analyzer.running || Analyzer.UpdateMode != UpdateMode.Tick)
    //        {
    //            return true;
    //        }

    //        if (Analyzer.loggingMode != LoggingMode.MiscTick)
    //        {
    //            return true;
    //        }


    //        var can = $"{__instance.pawn.thinker.ConstantThinkTree.defName} {__instance.pawn.Label}";
    //        Analyzer.Start(can);
    //        var result = __instance.DetermineNextConstantThinkTreeJob();
    //        if (result.Job != null)
    //        {
    //            thinkTree = __instance.pawn.thinker.ConstantThinkTree;
    //            __result = result;
    //            Analyzer.Stop(can);
    //            return false;
    //        }

    //        Analyzer.Stop(can);


    //        var result2 = ThinkResult.NoJob;
    //        try
    //        {
    //            var can2 = $"{__instance.pawn.thinker.MainThinkTree.defName} {__instance.pawn.Label}";
    //            result2 = __instance.pawn.thinker.MainThinkNodeRoot.TryIssueJobPackage(__instance.pawn, default);
    //            Analyzer.Stop(can2);
    //        }
    //        catch (Exception exception)
    //        {
    //            JobUtility.TryStartErrorRecoverJob(__instance.pawn,
    //                __instance.pawn.ToStringSafe() + " threw exception while determining job (main)", exception);
    //            thinkTree = null;
    //            __result = ThinkResult.NoJob;
    //            return false;
    //        }

    //        thinkTree = __instance.pawn.thinker.MainThinkTree;
    //        __result = result2;

    //        return false;
    //    }
    //}


    //[HarmonyPatch(typeof(ArmorUtility), nameof(ArmorUtility.GetPostArmorDamage))]
    //internal class H_GetPostArmorDamage
    //{
    //    public static void Prefix()
    //    {
    //        if (Analyzer.running && Analyzer.UpdateMode == UpdateMode.Tick)
    //        {
    //            if (Analyzer.loggingMode == LoggingMode.MiscTick)
    //            {
    //                Analyzer.Start("ArmorUtility GetPostArmorDamage");
    //            }
    //        }
    //    }

    //    public static void Postfix()
    //    {
    //        if (Analyzer.running)
    //        {
    //            if (Analyzer.UpdateMode == UpdateMode.Tick && Analyzer.loggingMode == LoggingMode.MiscTick)
    //            {
    //                Analyzer.Stop("ArmorUtility GetPostArmorDamage");
    //            }
    //        }
    //    }
    //}

    //[HarmonyPatch(typeof(DamageWorker_AddInjury), nameof(DamageWorker_AddInjury.ApplyDamageToPart))]
    //internal class H_ApplyDamageToPart
    //{
    //    public static void Prefix(ThingWithComps __instance)
    //    {
    //        if (Analyzer.running && Analyzer.UpdateMode == UpdateMode.Tick)
    //        {
    //            if (Analyzer.loggingMode == LoggingMode.MiscTick)
    //            {
    //                Analyzer.Start(__instance.GetType().Name, () => $"{__instance.GetType().Name} DamageWorker_AddInjury ApplyDamageToPart");
    //            }
    //        }
    //    }

    //    public static void Postfix(ThingWithComps __instance)
    //    {
    //        if (Analyzer.running)
    //        {
    //            if (Analyzer.UpdateMode == UpdateMode.Tick && Analyzer.loggingMode == LoggingMode.MiscTick)
    //            {
    //                Analyzer.Stop(__instance.GetType().Name);
    //            }
    //        }
    //    }
    //}


    //[HarmonyPatch(typeof(DamageWorker_AddInjury), nameof(DamageWorker_AddInjury.ApplyToPawn))]
    //internal class H_ApplyToPawn
    //{
    //    public static void Prefix(ThingWithComps __instance)
    //    {
    //        if (Analyzer.running && Analyzer.UpdateMode == UpdateMode.Tick)
    //        {
    //            if (Analyzer.loggingMode == LoggingMode.MiscTick)
    //            {
    //                Analyzer.Start(__instance.GetType().Name, () => $"{__instance.GetType().Name} DamageWorker_AddInjury ApplyToPawn");
    //            }
    //        }
    //    }

    //    public static void Postfix(ThingWithComps __instance)
    //    {
    //        if (Analyzer.running)
    //        {
    //            if (Analyzer.UpdateMode == UpdateMode.Tick && Analyzer.loggingMode == LoggingMode.MiscTick)
    //            {
    //                Analyzer.Stop(__instance.GetType().Name);
    //            }
    //        }
    //    }
    //}
    //[HarmonyPatch(typeof(DamageWorker), nameof(DamageWorker.Apply))]
    //internal class H_DamageWorkerApply
    //{
    //    public static void Prefix(ThingWithComps __instance)
    //    {
    //        if (Analyzer.running && Analyzer.UpdateMode == UpdateMode.Tick)
    //        {
    //            if (Analyzer.loggingMode == LoggingMode.MiscTick)
    //            {
    //                Analyzer.Start(__instance.GetType().Name, () => $"{__instance.GetType()} DamageWorker Apply");
    //            }
    //        }
    //    }

    //    public static void Postfix(ThingWithComps __instance)
    //    {
    //        if (Analyzer.running)
    //        {
    //            if (Analyzer.UpdateMode == UpdateMode.Tick && Analyzer.loggingMode == LoggingMode.MiscTick)
    //            {
    //                Analyzer.Stop(__instance.GetType().Name);
    //            }
    //        }
    //    }
    //}

    //[HarmonyPatch(typeof(ThinkNode_DutyConstant), nameof(ThinkNode_DutyConstant.TryIssueJobPackage))]
    //internal class H_DamageWorkerApply
    //{
    //    public static bool Prefix(ThinkNode_DutyConstant __instance, Pawn pawn, JobIssueParams jobParams, ref ThinkResult __result)
    //    {
    //        if (Analyzer.running && Analyzer.UpdateMode == UpdateMode.Tick)
    //        {
    //            if (Analyzer.loggingMode == LoggingMode.MiscTick)
    //            {
    //                if (pawn.GetLord() == null)
    //                {
    //                    Log.Error(pawn + " doing ThinkNode_DutyConstant with no Lord.", false);
    //                    __result = ThinkResult.NoJob;
    //                    return false;
    //                }
    //                if (pawn.mindState.duty == null)
    //                {
    //                    Log.Error(pawn + " doing ThinkNode_DutyConstant with no duty.", false);
    //                    __result = ThinkResult.NoJob;
    //                    return false;
    //                }
    //                if (__instance.dutyDefToSubNode == null)
    //                {
    //                    Log.Error(pawn + " has null dutyDefToSubNode. Recovering by calling ResolveSubnodes() (though that should have been called already).", false);
    //                    __instance.ResolveSubnodes();
    //                }
    //                int num = __instance.dutyDefToSubNode[pawn.mindState.duty.def];
    //                if (num < 0)
    //                {
    //                    __result = ThinkResult.NoJob;
    //                    return false;
    //                }
    //                var __state = string.Intern($"{__instance.subNodes[num]} from {__instance.GetType()}");
    //                Analyzer.Start(__state);
    //                __result = __instance.subNodes[num].TryIssueJobPackage(pawn, jobParams);
    //                Analyzer.Stop(__state);
    //                return false;
    //            }
    //        }
    //        return true;
    //    }

    //}

    //[HarmonyPatch(typeof(ThinkNode_JoinVoluntarilyJoinableLord), nameof(ThinkNode_JoinVoluntarilyJoinableLord.TryIssueJobPackage))]
    //internal class H_ThinkNode_JoinVoluntarilyJoinableLord
    //{
    //    public static bool Prefix(ThinkNode_JoinVoluntarilyJoinableLord __instance, Pawn pawn, JobIssueParams jobParams, ref ThinkResult __result, ref string __state)
    //    {
    //        if (Analyzer.running && Analyzer.UpdateMode == UpdateMode.Tick)
    //        {
    //            if (Analyzer.loggingMode == LoggingMode.MiscTick)
    //            {

    //                __state = string.Intern($"CheckLeaveCurrentVoluntarilyJoinableLord from {__instance.GetType()}");
    //                __instance.CheckLeaveCurrentVoluntarilyJoinableLord(pawn);
    //                Analyzer.Stop(__state);
    //                __state = string.Intern($"JoinVoluntarilyJoinableLord from {__instance.GetType()}");
    //                __instance.JoinVoluntarilyJoinableLord(pawn);
    //                Analyzer.Stop(__state);
    //                if (pawn.GetLord() != null && (pawn.mindState.duty == null || pawn.mindState.duty.def.hook == __instance.dutyHook))
    //                {
    //                    {
    //                        int count = __instance.subNodes.Count;
    //                        for (int i = 0; i < count; i++)
    //                        {
    //                            ThinkResult result = ThinkResult.NoJob;
    //                            try
    //                            {
    //                                __state = string.Intern($"{__instance.subNodes[i]} from {__instance.GetType()}");
    //                                result = __instance.subNodes[i].TryIssueJobPackage(pawn, jobParams);
    //                                Analyzer.Stop(__state);
    //                            }
    //                            catch (Exception ex)
    //                            {
    //                                Log.Error(string.Concat(new object[]
    //                                {
    //                                    "Exception in ",
    //                                    __instance.GetType(),
    //                                    " TryIssueJobPackage: ",
    //                                    ex.ToString()
    //                                }), false);
    //                            }
    //                            if (result.IsValid)
    //                            {
    //                                __result = result;
    //                                return false;
    //                            }
    //                        }
    //                        __result = ThinkResult.NoJob;
    //                        return false;
    //                    }
    //                }
    //                __result = ThinkResult.NoJob;
    //                return false;
    //            }
    //        }
    //        return true;
    //    }

    //}

    //[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.CheckForJobOverride))]
    //internal class H_CheckForJobOverride
    //{
    //    public static void Prefix(Pawn_JobTracker __instance)
    //    {
    //        if (Analyzer.running)
    //        {
    //            Log.Error(" CheckForJobOverride BRU");
    //        }
    //    }
    //}

    //[HarmonyPatch(typeof(ThingWithComps), nameof(Thing.TakeDamage))]
    //internal class H_TakeDamage
    //{
    //    public static void Prefix(ThingWithComps __instance, ref string __state)
    //    {
    //        if (Analyzer.running && Analyzer.UpdateMode == UpdateMode.Tick)
    //        {
    //            if (Analyzer.loggingMode == LoggingMode.MiscTick)
    //            {
    //                __state = string.Intern($"{__instance.GetType()} TakeDamage");
    //                Analyzer.Start(__state);
    //            }
    //        }
    //    }

    //    public static void Postfix(string __state)
    //    {
    //        if (__state != string.Empty)
    //        {
    //            Analyzer.Stop(__state);
    //        }
    //    }
    //}

    //[HarmonyPatch(typeof(Pawn), nameof(Pawn.PreApplyDamage))]
    //internal class H_PreApplyDamage
    //{
    //    public static void Prefix(ThingWithComps __instance)
    //    {
    //        if (Analyzer.running && Analyzer.UpdateMode == UpdateMode.Tick)
    //        {
    //            if (Analyzer.loggingMode == LoggingMode.MiscTick)
    //            {
    //                var name = string.Intern($"{__instance.GetType()} PreApplyDamage");
    //                Analyzer.Start(name, null, null);
    //            }
    //        }
    //    }

    //    public static void Postfix(ThingWithComps __instance)
    //    {
    //        if (Analyzer.running)
    //        {
    //            if (Analyzer.UpdateMode == UpdateMode.Tick && Analyzer.loggingMode == LoggingMode.MiscTick)
    //            {
    //                var name = string.Intern($"{__instance.GetType()} PreApplyDamage");
    //                Analyzer.Stop(name);
    //            }
    //        }
    //    }
    //}

    //[HarmonyPatch(typeof(Pawn), nameof(Pawn.PostApplyDamage))]
    //internal class H_PostApplyDamage
    //{
    //    public static void Prefix(ThingWithComps __instance)
    //    {
    //        if (Analyzer.running && Analyzer.UpdateMode == UpdateMode.Tick)
    //        {
    //            if (Analyzer.loggingMode == LoggingMode.MiscTick)
    //            {
    //                var name = string.Intern($"{__instance.GetType()} PostApplyDamage");
    //                Analyzer.Start(name, null, null);
    //            }
    //        }
    //    }

    //    public static void Postfix(ThingWithComps __instance)
    //    {
    //        if (Analyzer.running)
    //        {
    //            if (Analyzer.UpdateMode == UpdateMode.Tick && Analyzer.loggingMode == LoggingMode.MiscTick)
    //            {
    //                var name = string.Intern($"{__instance.GetType()} PostApplyDamage");
    //                Analyzer.Stop(name);
    //            }
    //        }
    //    }
    //}
}