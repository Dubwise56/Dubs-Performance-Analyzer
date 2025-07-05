using System.Reflection;
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


        public static bool Prefix(MethodBase __originalMethod, SectionLayer_Things __instance)
        {
            if (!Active)
                return true;

            if (!__instance.Visible)
                return false;

            var subMeshes = __instance.subMeshes;
            var count = subMeshes.Count;
            
            for (var i = 0; i < count; i++)
            {
                var layerSubMesh = subMeshes[i];
                if (layerSubMesh.finalized && !layerSubMesh.disabled)
                {
                    string Namer() => GetSubMeshName(layerSubMesh);

                    var prof = ProfileController.Start(GetSubMeshName(layerSubMesh), Namer, __originalMethod.GetType(),
                        __originalMethod);
                    
                    Graphics.DrawMesh(layerSubMesh.mesh, Vector3.zero, Quaternion.identity, layerSubMesh.material,
#if V1_5
                        0);
#else
                        layerSubMesh.renderLayer);
#endif
                    
                    prof.Stop();
                }
            }

            return false;
        }

        public static string GetSubMeshName(LayerSubMesh subMesh)
            => subMesh.material is var m
                && m
                && m.mainTexture is var mTex // todo material.mainTexture is a non trivial lookup
                && mTex
                && mTex.name is var mTexName
                && !string.IsNullOrEmpty(mTexName)
                    ? mTexName
                    : subMesh.GetType().Name;
    }
}