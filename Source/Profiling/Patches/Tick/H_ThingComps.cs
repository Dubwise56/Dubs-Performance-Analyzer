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
                // 1.6 moved most comp work into CompTickInterval, so CompTick alone misses the bulk of it
                foreach (var name in new[] { nameof(ThingComp.CompTick), nameof(ThingComp.CompTickRare), nameof(ThingComp.CompTickLong)
#if !V1_5
                    , nameof(ThingComp.CompTickInterval)
#endif
                })
                {
                    var method = AccessTools.Method(typ, name);

                    if (method != null && method.DeclaringType == typ)
                    {
                        yield return method;
                    }
                }
            }
        }
    }
}
