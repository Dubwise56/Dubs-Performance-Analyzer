using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace Analyzer.Profiling.Patches.Tick
{
    [Entry("entry.tick.thingcomp", Category.Tick)]
    internal static class H_ThingComps
    {
        public static bool Active = false;

        public static IEnumerable<MethodInfo> GetPatchMethods()
        {
            foreach (var typ in typeof(ThingComp).AllSubclasses())
            {
                var method =  AccessTools.Method(typ, nameof(ThingComp.CompTick));

                if (method != null && method.DeclaringType == typ)
                {
                    yield return method;
                }
            }
        }
    }
}
