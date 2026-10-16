using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Analyzer.Profiling
{
	[Entry("entry.update.mapdrawer", Category.Update)]
	internal class H_DrawSection
	{
		public static bool Active = false;

		[Setting("By Def")] public static bool ByDef = false;

		public static void ProfilePatch()
		{
			original = AccessTools.Method(typeof(Section), nameof(Section.DrawSection));
			Modbase.Harmony.Patch(original, transpiler: new HarmonyMethod(typeof(H_DrawSection), nameof(Transpiler)));
		}

		private static MethodInfo original;

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
			=> CallSiteProfiling.WrapCalls(instructions, __originalMethod,
				m => m.Name == nameof(SectionLayer.DrawLayer) && m.GetParameters().Length == 0,
				AccessTools.Method(typeof(H_DrawSection), nameof(DrawLayer)));

		public static void DrawLayer(SectionLayer layer)
		{
			if (!Active)
			{
				layer.DrawLayer();
				return;
			}

			var type = layer.GetType();
			var prof = ProfileController.Start(type.Name, null, type, original);
			layer.DrawLayer();
			prof?.Stop();
		}
	}
}
