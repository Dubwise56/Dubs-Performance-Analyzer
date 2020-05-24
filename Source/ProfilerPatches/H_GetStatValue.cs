using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace DubsAnalyzer
{
    [ProfileMode("Stats", UpdateMode.Update)]
    internal class H_GetStatValue
    {
        public static bool Active = false;

        //[Setting("Stat Parts")]
        //public static bool StatParts = false;
        [Setting("By Def")]
        public static bool ByDef = false;

        [Setting("Get Value Detour")]
        public static bool GetValDetour = false;

        public static void ProfilePatch()
        {
            Log.Message("Patching stats");
            var jiff = AccessTools.Method(typeof(StatExtension), nameof(StatExtension.GetStatValue));
            var pre = new HarmonyMethod(typeof(H_GetStatValue), nameof(Prefix));
            var post = new HarmonyMethod(typeof(H_GetStatValue), nameof(Postfix));
            Analyzer.harmony.Patch(jiff, pre, post);


            jiff = AccessTools.Method(typeof(StatExtension), nameof(StatExtension.GetStatValueAbstract), new []{ typeof(BuildableDef), typeof(StatDef), typeof(ThingDef) });
            pre = new HarmonyMethod(typeof(H_GetStatValue), nameof(PrefixAb));
            Analyzer.harmony.Patch(jiff, pre, post);

            jiff = AccessTools.Method(typeof(StatExtension), nameof(StatExtension.GetStatValueAbstract), new []{ typeof(AbilityDef), typeof(StatDef) });
            pre = new HarmonyMethod(typeof(H_GetStatValue), nameof(PrefixAbility));
            Analyzer.harmony.Patch(jiff, pre, post);

            jiff = AccessTools.Method(typeof(StatWorker), nameof(StatWorker.GetValue), new []{ typeof(StatRequest), typeof(bool) });
            pre = new HarmonyMethod(typeof(H_GetStatValue), nameof(GetValueDetour));
            Analyzer.harmony.Patch(jiff, pre);


            var go = new HarmonyMethod(typeof(H_GetStatValue), nameof(PartPrefix));
            var biff = new HarmonyMethod(typeof(H_GetStatValue), nameof(PartPostfix));

            foreach (var allLeafSubclass in typeof(StatPart).AllSubclassesNonAbstract())
            {
                try
                {
                    var mef = AccessTools.Method(allLeafSubclass, nameof(StatPart.TransformValue));
                    if (mef.DeclaringType == allLeafSubclass)
                    {
                        var info = Harmony.GetPatchInfo(mef);
                        var F = true;
                        if (info != null)
                        {
                            foreach (var infoPrefix in info.Prefixes)
                            {
                                if (infoPrefix.PatchMethod == go.method)
                                {
                                    F = false;
                                }
                            }
                        }

                        if (F)
                        {
                            Analyzer.harmony.Patch(mef, go, biff);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to patch {allLeafSubclass} from {allLeafSubclass.Assembly.FullName} for profiling");
                }

            }

            Log.Message("stats patched");
        }

        public static void PartPrefix(object __instance, MethodBase __originalMethod, ref string __state)
        {
            if (Active)
            {
                if (__originalMethod.ReflectedType != null)
                {
                    __state = __originalMethod.ReflectedType.ToString();
                }
                else
                {
                    __state = __originalMethod.GetType().ToString();
                }

                Analyzer.Start(__state);
            }
        }

        public static void PartPostfix(string __state)
        {
            if (Active)
            {
                Analyzer.Stop(__state);
            }
        }

        public static bool GetValueDetour(StatWorker __instance, StatRequest req, ref float __result, bool applyPostProcess = true)
        {
            if (Active && GetValDetour && __instance is StatWorker sw)
            {
                if (sw.stat.minifiedThingInherits)
                {
                    if (req.Thing is MinifiedThing minifiedThing)
                    {
                        if (minifiedThing.InnerThing == null)
                        {
                            Log.Error("MinifiedThing's inner thing is null.");
                        }
                        __result = minifiedThing.InnerThing.GetStatValue(sw.stat, applyPostProcess);
                        return false;
                    }
                }

                var slag = "";
                if (ByDef)
                {
                    slag = $"{__instance.stat.defName} GetValueUnfinalized for {req.Def.defName}";
                }
                else
                {
                    slag = $"{__instance.stat.defName} GetValueUnfinalized";
                }

                Analyzer.Start(slag);
                float valueUnfinalized = sw.GetValueUnfinalized(req, applyPostProcess);
                Analyzer.Stop(slag);

                if (ByDef)
                {
                    slag = $"{__instance.stat.defName} FinalizeValue for {req.Def.defName}";
                }
                else
                {
                    slag = $"{__instance.stat.defName} FinalizeValue";
                }
                
                Analyzer.Start(slag);
                sw.FinalizeValue(req, ref valueUnfinalized, applyPostProcess);
                Analyzer.Stop(slag);

                __result = valueUnfinalized;
                return false;
            }
            return true;
        }

        public static bool Prefix(Thing thing, StatDef stat, ref string __state)
        {
            if (Active && !GetValDetour)
            {
                if (ByDef)
                {
                    __state = $"{stat.defName} for {thing.def.defName}";
                }
                else
                {
                    __state = stat.defName;
                }

                Analyzer.Start(__state);
            }

            return true;
        }

        public static void Postfix(string __state)
        {
            if (Active && !GetValDetour)
            {
                Analyzer.Stop(__state);
            }
        }

        public static bool PrefixAb(BuildableDef def, StatDef stat, ref string __state)
        {

            if (Active && !GetValDetour)
            {
                if (ByDef)
                {
                    __state = $"{stat.defName} abstract for {def.defName}";
                }
                else
                {
                    __state = $"{stat.defName} abstract";
                }
               
                Analyzer.Start(__state);
            }
            return true;
        }

        public static bool PrefixAbility(AbilityDef def, StatDef stat, ref string __state)
        {

            if (Active && !GetValDetour)
            {
                if (ByDef)
                {
                    __state = $"{stat.defName} abstract for {def.defName}";
                }
                else
                {
                    __state = $"{stat.defName} abstract";
                }
               
                Analyzer.Start(__state);
            }
            return true;
        }

    }
}