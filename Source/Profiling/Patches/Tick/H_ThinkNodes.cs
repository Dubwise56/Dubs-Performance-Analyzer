using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI;

namespace Analyzer.Profiling
{
    [Entry("entry.tick.thinknodes", Category.Tick)]
    internal static class H_ThinkNodes
    {
        public static bool Active = false;

        public static IEnumerable<MethodInfo> GetPatchMethods()
        {
            foreach (var typ in typeof(ThinkNode).AllSubclasses())
            {
                var method =  AccessTools.Method(typ, nameof(ThinkNode.TryIssueJobPackage));

                if (method != null && method.DeclaringType == typ)
                {
                    yield return method;
                }
            }
        }
    }
}