using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace DubsAnalyzer
{
    [ProfileMode("ThinkNodes", UpdateMode.Tick)]
    internal static class H_ThinkNodes
    {
        public static bool Active = false;
        public static List<MethodInfo> patched = new List<MethodInfo>();
        public static void ProfilePatch()
        {
            var go = new HarmonyMethod(typeof(H_ThinkNodes), nameof(Prefix));
            var biff = new HarmonyMethod(typeof(H_ThinkNodes), nameof(Postfix));

            //foreach (var allLeafSubclass in typeof(ThinkNode).AllSubclassesNonAbstract())
            //{
            //    var mef = AccessTools.Method(allLeafSubclass, nameof(ThinkNode.TryIssueJobPackage));

            //    if (!mef.DeclaringType.IsAbstract && mef.DeclaringType == allLeafSubclass)
            //    {
            //        if (!patched.Contains(mef))
            //        {
            //            Analyzer.harmony.Patch(mef, go, biff);
            //            patched.Add(mef);
            //        }
            //    }
            //}

            foreach (var typ in GenTypes.AllTypes)
            {
                if (typeof(ThinkNode_JobGiver).IsAssignableFrom(typ))
                {
                    var trygive = AccessTools.Method(typ, nameof(ThinkNode_JobGiver.TryGiveJob));
                    if (!trygive.DeclaringType.IsAbstract && trygive.DeclaringType == typ)
                    {
                        if (!patched.Contains(trygive))
                        {
                            Analyzer.harmony.Patch(trygive, go, biff);
                            patched.Add(trygive);
                        }
                    }
                }
                else
                if (typeof(ThinkNode).IsAssignableFrom(typ))
                {
                    var mef = AccessTools.Method(typ, nameof(ThinkNode.TryIssueJobPackage));

                    if (!mef.DeclaringType.IsAbstract && mef.DeclaringType == typ)
                    {
                        if (!patched.Contains(mef))
                        {
                            Analyzer.harmony.Patch(mef, go, biff);
                            patched.Add(mef);
                        }
                    }
                }
            }
        }

        [HarmonyPriority(Priority.Last)]
        public static void Prefix(object __instance, MethodBase __originalMethod, ref string __state)
        {
            if (Active)
            {
                if (__instance != null)
                {
                    __state = __instance.GetType().Name;
                }
                else
                if (__originalMethod.ReflectedType != null)
                {
                    __state = __originalMethod.ReflectedType.Name;
                }
                else
                {
                    __state = __originalMethod.GetType().Name;
                }

                Analyzer.Start(__state, null, null, null, null, __originalMethod as MethodInfo);
            }
        }

        [HarmonyPriority(Priority.First)]
        public static void Postfix(string __state)
        {
            if (Active)
            {
                Analyzer.Stop(__state);
            }
        }
    }
}