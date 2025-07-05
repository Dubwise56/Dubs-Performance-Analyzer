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

		public static bool Prefix(MethodBase __originalMethod, Section __instance)
		{
			if (!Active)
				return true;
			
			if (__instance.anyLayerDirty)
				__instance.RegenerateDirtyLayers();
			
			for (int index = 0; index < __instance.layers.Count; ++index)
			{
				var layer = __instance.layers[index];
				var type = layer.GetType();
				var prof = ProfileController.Start(type.Name, null, type, __originalMethod);
				layer.DrawLayer();
				prof.Stop();
			}

			if (!DebugViewSettings.drawSectionEdges)
				return false;
			
			Vector3 vector3_1 = __instance.botLeft.ToVector3();
			GenDraw.DrawLineBetween(vector3_1, vector3_1 + new Vector3(0.0f, 0.0f, 17f));
			GenDraw.DrawLineBetween(vector3_1, vector3_1 + new Vector3(17f, 0.0f, 0.0f));
			
			if (!__instance.CellRect.Contains(UI.MouseCell()))
				return false;
			
			IntVec3 intVec3 = __instance.bounds.Min;
			Vector3 vector3_2 = intVec3.ToVector3();
			intVec3 = __instance.bounds.Max;
			Vector3 A = intVec3.ToVector3() + new Vector3(1f, 0.0f, 1f);
			GenDraw.DrawLineBetween(vector3_2, vector3_2 + new Vector3((float)__instance.bounds.Width, 0.0f, 0.0f),
				SimpleColor.Magenta);
			GenDraw.DrawLineBetween(vector3_2, vector3_2 + new Vector3(0.0f, 0.0f, (float)__instance.bounds.Height),
				SimpleColor.Magenta);
			GenDraw.DrawLineBetween(A, A - new Vector3((float)__instance.bounds.Width, 0.0f, 0.0f),
				SimpleColor.Magenta);
			GenDraw.DrawLineBetween(A, A - new Vector3(0.0f, 0.0f, (float)__instance.bounds.Height),
				SimpleColor.Magenta);

			return false;
		}
	}
}
