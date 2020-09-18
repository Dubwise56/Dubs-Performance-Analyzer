using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Verse.AI;

namespace Analyzer.Profiling
{
    [Entry("Job AI", Category.Update)]
    public class H_JobAI
    {
        public static bool Active = false;

        public static IEnumerable<MethodInfo> GetPatchMethods() => Utility.GetTypeMethods(typeof(Pawn_JobTracker));
    }
}
