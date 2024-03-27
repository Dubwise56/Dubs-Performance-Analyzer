using System.Reflection;
using HarmonyLib;
using UnityEngine;
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
			Modbase.Harmony.Patch(AccessTools.Method(typeof(Section), nameof(Section.DrawSection)),
				new HarmonyMethod(typeof(H_DrawSection), "Prefix"));
		}

		public static bool Prefix(MethodBase __originalMethod, Section __instance, bool drawSunShadowsOnly)
		{
			if (!Active)
			{
				return true;
			}

			if (drawSunShadowsOnly)
			{
				__instance.layerSunShadows.DrawLayer();
			}
			else
			{
				int count = __instance.layers.Count;
				for (int i = 0; i < count; i++)
				{
					var type = __instance.layers[i].GetType();
					var name = type.Name;

					var prof = ProfileController.Start(name, null, type, __originalMethod);
					__instance.layers[i].DrawLayer();
					prof.Stop();
				}
			}
			bool flag = !drawSunShadowsOnly && DebugViewSettings.drawSectionEdges;
			if (flag)
			{
				GenDraw.DrawLineBetween(__instance.botLeft.ToVector3(), __instance.botLeft.ToVector3() + new Vector3(0f, 0f, 17f));
				GenDraw.DrawLineBetween(__instance.botLeft.ToVector3(), __instance.botLeft.ToVector3() + new Vector3(17f, 0f, 0f));
			}

			return false;
		}
	}
}
