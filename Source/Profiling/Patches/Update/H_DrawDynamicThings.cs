using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Analyzer.Profiling
{
    [Entry("entry.update.dynamicdraw", Category.Update)]
    internal class H_DrawDynamicThings
    {
        public static bool Active = false;

        [Setting("By Def")]
        public static bool ByDef = false;

        public static void ProfilePatch()
        {
            original = AccessTools.Method(typeof(DynamicDrawManager), nameof(DynamicDrawManager.DrawDynamicThings));
            Modbase.Harmony.Patch(original, transpiler: new HarmonyMethod(typeof(H_DrawDynamicThings), nameof(Transpiler)));
        }

        private static MethodInfo original;

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
            => CallSiteProfiling.WrapCalls(instructions, __originalMethod,
                AccessTools.Method(typeof(Thing), nameof(Thing.DynamicDrawPhase)), AccessTools.Method(typeof(H_DrawDynamicThings), nameof(DynamicDrawPhase)));

        public static void DynamicDrawPhase(Thing thing, DrawPhase phase)
        {
            if (!Active)
            {
                thing.DynamicDrawPhase(phase);
                return;
            }

            string Namer()
                => ByDef
                    ? $"{thing.def.defName} - {thing?.def?.modContentPack?.Name}"
                    : thing.GetType().ToString();

            var prof = ProfileController.Start(ByDef ? thing.def.defName : thing.GetType().Name, Namer,
                thing.GetType(), original);
            thing.DynamicDrawPhase(phase);
            prof?.Stop();
        }
    }
}