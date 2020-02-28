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

        public static void ProfilePatch()
        {
            var go = new HarmonyMethod(typeof(H_ThinkNodes), nameof(Prefix));
            var biff = new HarmonyMethod(typeof(H_ThinkNodes), nameof(Postfix));

            foreach (var allLeafSubclass in typeof(ThinkNode).AllSubclasses())
            {
                var mef = AccessTools.Method(allLeafSubclass, nameof(ThinkNode.TryIssueJobPackage));

                if (!mef.DeclaringType.IsAbstract && mef.DeclaringType == allLeafSubclass)
                {
                    Analyzer.harmony.Patch(mef, go, biff);
                }
            }
        }
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

                Analyzer.Start(__state);
            }
        }

        public static void Postfix(string __state)
        {
            if (Active)
            {
                Analyzer.Stop(__state);
            }
        }
    }
}