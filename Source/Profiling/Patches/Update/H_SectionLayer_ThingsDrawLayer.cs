﻿using System.Reflection;

using HarmonyLib;

using UnityEngine;
using UnityEngine.Rendering;

using Verse;

namespace Analyzer.Profiling
{
	[Entry("entry.update.sectionlayer.thingsdrawlayer", Category.Update)]
	internal class H_SectionLayer_ThingsDrawLayer
	{
		public static bool Active = false;

		public static void ProfilePatch()
		{
			Modbase.Harmony.Patch(AccessTools.Method(typeof(SectionLayer_Things), nameof(SectionLayer_Things.DrawLayer)),
				new HarmonyMethod(typeof(H_SectionLayer_ThingsDrawLayer), "Prefix"));
		}

		static Matrix4x4 johnmatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);


		public static bool Prefix(MethodBase __originalMethod, SectionLayer_Things __instance)
		{
			if (!Active)
			{
				return true;
			}

			if (!__instance.Visible)
			{
				return false;
			}

			int count = __instance.subMeshes.Count;

			for (int i = 0; i < count; i++)
			{
				LayerSubMesh layerSubMesh = __instance.subMeshes[i];
				if (layerSubMesh.finalized && !layerSubMesh.disabled)
				{
					string Namer()
					{
						var n = layerSubMesh.material?.mainTexture?.name ?? layerSubMesh.GetType().Name;
						return n;
					}

					var name = layerSubMesh.material?.mainTexture?.name ?? layerSubMesh.GetType().Name;

					var prof = ProfileController.Start(name, Namer, __originalMethod.GetType(), __originalMethod);
					Graphics.DrawMesh(layerSubMesh.mesh, Matrix4x4.identity, layerSubMesh.material, 0);
					prof.Stop();
				}
			}


			return false;
		}
	}
}