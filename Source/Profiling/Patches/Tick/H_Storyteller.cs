using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace Analyzer.Profiling
{
    [Entry("entry.tick.storyteller", Category.Tick)]
    public class H_Storyteller
    {
        public static bool Active = false;

        public static IEnumerable<MethodInfo> GetPatchMethods()
        {
            foreach(var tm in Utility.GetTypeMethods(typeof(Storyteller), true))
                yield return tm;
            
            foreach(var meth in typeof(IncidentWorker).AllSubnBaseImplsOf((t) => AccessTools.Method(t, nameof(IncidentWorker.CanFireNowSub))))
                yield return meth;

            foreach(var meth in typeof(IncidentWorker).AllSubnBaseImplsOf((t) => AccessTools.Method(t, nameof(IncidentWorker.TryExecuteWorker))))
                yield return meth;

            yield return  AccessTools.Method(typeof(QuestUtility), nameof(QuestUtility.GenerateQuestAndMakeAvailable), new[] {typeof(QuestScriptDef), typeof(Slate)});
        }
    }
}