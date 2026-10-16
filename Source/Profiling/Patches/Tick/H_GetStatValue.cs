using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Analyzer.Profiling
{
    [Entry("entry.tick.stats", Category.Tick)]
    internal class H_GetStatValue
    {
        public static bool Active = false;

        [Setting("By Def")]
        public static bool ByDef = false;

        [Setting("Get Value Detour")]
        public static bool GetValDetour = false;

        public static void ProfilePatch()
        {
            var jiff = AccessTools.Method(typeof(StatExtension), nameof(StatExtension.GetStatValueAbstract), new[] { typeof(BuildableDef), typeof(StatDef), typeof(ThingDef) });
            var pre = new HarmonyMethod(typeof(H_GetStatValue), nameof(PrefixAb));
            var post = new HarmonyMethod(typeof(H_GetStatValue), nameof(Postfix));

            Modbase.Harmony.Patch(jiff, pre, post);

            jiff = AccessTools.Method(typeof(StatExtension), nameof(StatExtension.GetStatValueAbstract), new[] { typeof(AbilityDef), typeof(StatDef), typeof(Pawn) });
            pre = new HarmonyMethod(typeof(H_GetStatValue), nameof(PrefixAbility));
            Modbase.Harmony.Patch(jiff, pre, post);

            getValue = AccessTools.Method(typeof(StatWorker), nameof(StatWorker.GetValue), new[] { typeof(StatRequest), typeof(bool) });
            Modbase.Harmony.Patch(getValue, transpiler: new HarmonyMethod(typeof(H_GetStatValue), nameof(GetValueTranspiler)));

            HarmonyMethod go = new HarmonyMethod(typeof(H_GetStatValue), nameof(PartPrefix));
            HarmonyMethod biff = new HarmonyMethod(typeof(H_GetStatValue), nameof(PartPostfix));

            foreach (Type allLeafSubclass in typeof(StatPart).AllSubclassesNonAbstract())
            {
                try
                {
                    MethodInfo mef = AccessTools.Method(allLeafSubclass, nameof(StatPart.TransformValue), new Type[] { typeof(StatRequest), typeof(float).MakeByRefType() });
                    if (mef.DeclaringType == allLeafSubclass)
                    {
                        HarmonyLib.Patches info = Harmony.GetPatchInfo(mef);
                        bool F = true;
                        if (info != null)
                        {
                            foreach (Patch infoPrefix in info.Prefixes)
                            {
                                if (infoPrefix.PatchMethod == go.method)
                                {
                                    F = false;
                                }
                            }
                        }

                        if (F)
                        {
                            Modbase.Harmony.Patch(mef, go, biff);
                        }
                    }
                }
                catch (Exception e)
                {
                    ThreadSafeLogger.ReportException(e, $"Failed to patch {allLeafSubclass} from {allLeafSubclass.Assembly.FullName} for profiling");
                }

            }
        }

        [HarmonyPriority(Priority.Last)]
        public static void PartPrefix(object __instance, MethodBase __originalMethod, ref Profiler __state)
        {
            if (Active)
            {
                string state = string.Empty;
                if (__originalMethod.ReflectedType != null)
                {
                    state = __originalMethod.ReflectedType.ToString();
                }
                else
                {
                    state = __originalMethod.GetType().ToString();
                }

                __state = ProfileController.Start(state, null, null, __originalMethod);
            }
        }

        [HarmonyPriority(Priority.First)]
        public static void PartPostfix(Profiler __state)
        {
            if (Active)
            {
                __state.Stop();
            }
        }

        private static MethodInfo getValue;

        private static IEnumerable<CodeInstruction> GetValueTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            instructions = CallSiteProfiling.WrapCalls(instructions, __originalMethod,
                AccessTools.Method(typeof(StatWorker), nameof(StatWorker.GetValueUnfinalized)),
                AccessTools.Method(typeof(H_GetStatValue), nameof(GetValueUnfinalized)));
            return CallSiteProfiling.WrapCalls(instructions, __originalMethod,
                AccessTools.Method(typeof(StatWorker), nameof(StatWorker.FinalizeValue)),
                AccessTools.Method(typeof(H_GetStatValue), nameof(FinalizeValue)));
        }

        public static float GetValueUnfinalized(StatWorker sw, StatRequest req, bool applyPostProcess)
        {
            if (!Active || !GetValDetour) return sw.GetValueUnfinalized(req, applyPostProcess);

            var slag = ByDef
                ? $"{sw.stat.defName} GetValueUnfinalized for {req.Def.defName}"
                : $"{sw.stat.defName} GetValueUnfinalized";

            Profiler prof = ProfileController.Start(slag, null, null, getValue);
            float valueUnfinalized = sw.GetValueUnfinalized(req, applyPostProcess);
            prof?.Stop();
            return valueUnfinalized;
        }

        public static void FinalizeValue(StatWorker sw, StatRequest req, ref float val, bool applyPostProcess)
        {
            if (!Active || !GetValDetour)
            {
                sw.FinalizeValue(req, ref val, applyPostProcess);
                return;
            }

            var slag = ByDef
                ? $"{sw.stat.defName} FinalizeValue for {req.Def.defName}"
                : $"{sw.stat.defName} FinalizeValue";

            Profiler prof = ProfileController.Start(slag, null, null, getValue);
            sw.FinalizeValue(req, ref val, applyPostProcess);
            prof?.Stop();
        }


        public static IEnumerable<MethodInfo> GetPatchMethods()
        {
            yield return AccessTools.Method(typeof(StatExtension), nameof(StatExtension.GetStatValue));
        }

        public static string GetName(Thing thing, StatDef stat)
        {
            return GetValDetour ? null : 
                ByDef ? $"{stat.defName} for {thing.def.defName}" : stat.defName;
        }


        [HarmonyPriority(Priority.First)]
        public static void Postfix(Profiler __state)
        {
            if (Active && !GetValDetour)
            {
                __state?.Stop();
            }
        }

        [HarmonyPriority(Priority.Last)]
        public static void PrefixAb(MethodBase __originalMethod, BuildableDef def, StatDef stat, ref Profiler __state)
        {

            if (Active && !GetValDetour)
            {
                string state = string.Empty;
                if (ByDef)
                {
                    state = $"{stat.defName} abstract for {def.defName}";
                }
                else
                {
                    state = $"{stat.defName} abstract";
                }

                __state = ProfileController.Start(state, null, null, __originalMethod);
            }
        }

        [HarmonyPriority(Priority.Last)]
        public static void PrefixAbility(MethodBase __originalMethod, AbilityDef def, StatDef stat, ref Profiler __state)
        {

            if (Active && !GetValDetour)
            {
                string state = string.Empty;
                if (ByDef)
                {
                    state = $"{stat.defName} abstract for {def.defName}";
                }
                else
                {
                    state = $"{stat.defName} abstract";
                }

                __state = ProfileController.Start(state, null, null, __originalMethod);
            }
        }

    }
}