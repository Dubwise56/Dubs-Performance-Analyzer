using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
	//TODO transpile this.
	[Entry("entry.update.mapdrawer", Category.Update)]
	internal class H_DrawSection
	{
		public static bool Active = false;

		[Setting("By Def")] public static bool ByDef = false;

		public static void ProfilePatch()
		{
			Modbase.Harmony.Patch(AccessTools.Method(typeof(Section), nameof(Section.DrawSection)),
				new HarmonyMethod(typeof(H_DrawSection), "Prefix"));
		}

		public static bool Prefix(MethodBase __originalMethod, Section __instance)
		{
			if (!Active)
			{
				return true;
			}

			if (__instance.anyLayerDirty)
			{
				__instance.RegenerateDirtyLayers();
			}
			
				int count = __instance.layers.Count;
				for (int i = 0; i < count; i++)
				{
					var type = __instance.layers[i].GetType();
					var name = type.Name;

					var prof = ProfileController.Start(name, null, type, __originalMethod);
					__instance.layers[i].DrawLayer();
					prof.Stop();
				}
			
			if (DebugViewSettings.drawSectionEdges)
			{
				Vector3 a = __instance.botLeft.ToVector3();
				GenDraw.DrawLineBetween(a, a + new Vector3(0f, 0f, 17f));
				GenDraw.DrawLineBetween(a, a + new Vector3(17f, 0f, 0f));
				if (__instance.CellRect.Contains(UI.MouseCell()))
				{
					Vector3 a2 = __instance.bounds.Min.ToVector3();
					Vector3 a3 = __instance.bounds.Max.ToVector3() + new Vector3(1f, 0f, 1f);
					GenDraw.DrawLineBetween(a2, a2 + new Vector3(__instance.bounds.Width, 0f, 0f), SimpleColor.Magenta);
					GenDraw.DrawLineBetween(a2, a2 + new Vector3(0f, 0f, __instance.bounds.Height), SimpleColor.Magenta);
					GenDraw.DrawLineBetween(a3, a3 - new Vector3(__instance.bounds.Width, 0f, 0f), SimpleColor.Magenta);
					GenDraw.DrawLineBetween(a3, a3 - new Vector3(0f, 0f, __instance.bounds.Height), SimpleColor.Magenta);
				}
			}

			return false;
		}
	}
}
