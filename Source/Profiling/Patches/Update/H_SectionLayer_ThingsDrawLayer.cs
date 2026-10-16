using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    [Entry("entry.update.sectionlayer.thingsdrawlayer", Category.Update)]
    internal class H_SectionLayer_ThingsDrawLayer
    {
        public static bool Active = false;

        public static void ProfilePatch()
        {
            var drawLayer = AccessTools.Method(typeof(SectionLayer), nameof(SectionLayer.DrawLayer));
            original = AccessTools.DeclaredMethod(drawLayer.DeclaringType, drawLayer.Name, Type.EmptyTypes);
            Modbase.Harmony.Patch(original, transpiler: new HarmonyMethod(typeof(H_SectionLayer_ThingsDrawLayer), nameof(Transpiler)));
        }

        private static MethodInfo original;

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
            => CallSiteProfiling.WrapCalls(instructions, __originalMethod,
                m => m.DeclaringType == typeof(Graphics) && m.Name == nameof(Graphics.DrawMesh),
                AccessTools.Method(typeof(H_SectionLayer_ThingsDrawLayer), nameof(DrawMesh)), passInstance: true);

#if V1_7
        public static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer, object __instance)
#else
        public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, object __instance)
#endif
        {
            if (!Active || __instance is not SectionLayer_Things)
            {
#if V1_7
                Graphics.DrawMesh(mesh, matrix, material, layer);
#else
                Graphics.DrawMesh(mesh, position, rotation, material, layer);
#endif
                return;
            }

            var name = GetMaterialName(material);
            var prof = ProfileController.Start(name, null, null, original);
#if V1_7
            Graphics.DrawMesh(mesh, matrix, material, layer);
#else
            Graphics.DrawMesh(mesh, position, rotation, material, layer);
#endif
            prof?.Stop();
        }

        public static string GetMaterialName(Material m)
            => m
                && m.HasProperty("_MainTex") // Unity logs a warning per call otherwise (e.g. the SunShadowFade material)
                && m.mainTexture is var mTex // todo material.mainTexture is a non trivial lookup
                && mTex
                && mTex.name is var mTexName
                && !string.IsNullOrEmpty(mTexName)
                    ? mTexName
                    : nameof(LayerSubMesh);
    }
}