using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Analyzer.Profiling
{
    [Entry("entry.update.thoughtworker", Category.Update)]
    public class H_ThoughtWorkers
    {
        public static bool Active = false;


        public static IEnumerable<MethodInfo> GetPatchMethods()
        {
            foreach (var type in typeof(ThoughtWorker).AllSubclasses())
            {
                var method = AccessTools.Method(type, "CurrentStateInternal");
                if(method.DeclaringType == type) yield return method;

                method = AccessTools.Method(type, "CurrentSocialStateInternal");
                if(method.DeclaringType == type) yield return method;
            }
        }
    }
}
