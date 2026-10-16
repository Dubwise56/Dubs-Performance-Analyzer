using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Analyzer.Profiling
{
    [Entry("entry.update.sectionlayerthings", Category.Update)]
    internal class H_SectionLayer_Things
    {
        public static bool Active = false;

        public static void ProfilePatch()
        {
            original = AccessTools.Method(typeof(SectionLayer_Things), nameof(SectionLayer_Things.Regenerate));
            Modbase.Harmony.Patch(original, transpiler: new HarmonyMethod(typeof(H_SectionLayer_Things), nameof(Transpiler)));
        }

        private static MethodInfo original;

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            instructions = CallSiteProfiling.WrapCalls(instructions, __originalMethod,
                AccessTools.Method(typeof(SectionLayer_Things), nameof(SectionLayer_Things.TakePrintFrom)),
                AccessTools.Method(typeof(H_SectionLayer_Things), nameof(TakePrintFrom)));
            return CallSiteProfiling.WrapCalls(instructions, __originalMethod,
                m => m.Name == nameof(SectionLayer.FinalizeMesh),
                AccessTools.Method(typeof(H_SectionLayer_Things), nameof(FinalizeMesh)));
        }

        public static void TakePrintFrom(SectionLayer_Things layer, Thing t)
        {
            if (!Active)
            {
                layer.TakePrintFrom(t);
                return;
            }

            var prof = ProfileController.Start(t.def.defName, null, null, original);
            layer.TakePrintFrom(t);
            prof?.Stop();
        }

        public static void FinalizeMesh(SectionLayer layer, MeshParts tags)
        {
            if (!Active)
            {
                layer.FinalizeMesh(tags);
                return;
            }

            var prof = ProfileController.Start("Finalize mesh", null, null, original);
            layer.FinalizeMesh(tags);
            prof?.Stop();
        }
    }
}