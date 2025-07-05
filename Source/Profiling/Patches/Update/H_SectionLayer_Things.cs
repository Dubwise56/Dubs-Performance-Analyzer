using System;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace Analyzer.Profiling
{
    [Entry("entry.update.sectionlayerthings", Category.Update)]
    internal class H_SectionLayer_Things
    {
        public static bool Active = false;

        public static void ProfilePatch()
        {
            Modbase.Harmony.Patch(
                AccessTools.Method(typeof(SectionLayer_Things), nameof(SectionLayer_Things.Regenerate)),
                new HarmonyMethod(typeof(H_SectionLayer_Things), nameof(Prefix)));
        }

        public static bool Prefix(MethodBase __originalMethod, SectionLayer_Things __instance, ref string __state)
        {
            if (Active)
            {
                __instance.ClearSubMeshes(MeshParts.All);
                Profiler prof = null;
                __instance.bounds = __instance.section.CellRect;
                foreach (IntVec3 c in __instance.section.CellRect)
                {
                    List<Thing> thingList = __instance.Map.thingGrid.ThingsListAt(c);
                    int count = thingList.Count;
                    for (int index = 0; index < count; ++index)
                    {
                        Thing t = thingList[index];
                        __state = "Flag check";
                        prof = ProfileController.Start(__state, null, null, __originalMethod);

                        var flag = (t.def.seeThroughFog || !__instance.Map.fogGrid.IsFogged(t.Position))
                            && t.def.drawerType != DrawerType.None
                            && (t.def.drawerType != DrawerType.RealtimeOnly || !__instance.requireAddToMapMesh)
#if V1_5
                            && ((double)t.def.hideAtSnowDepth >= 1.0
                                || __instance.Map.snowGrid.GetDepth(t.Position) <= (double)t.def.hideAtSnowDepth)
#else
                            && (t.def.hideAtSnowOrSandDepth >= 1f
                                || !(Math.Max(__instance.Map.snowGrid.GetDepth(t.Position),
                                        t.Position.GetSandDepth(__instance.Map))
                                    > t.def.hideAtSnowOrSandDepth))
                            && (t.def.plant == null
                                || t.def.plant.showInFrozenWater
                                || t.Position.GetTerrain(__instance.Map) != TerrainDefOf.ThinIce)
#endif
                            && t.Position.x == c.x
                            && t.Position.z == c.z;
                        
                        prof.Stop();
                        
                        if (flag)
                        {
                            __state = t.def.defName;
                            prof = ProfileController.Start(__state, null, null, __originalMethod);
                            __instance.TakePrintFrom(t);
                            __instance.bounds = __instance.bounds.Encapsulate(t.OccupiedDrawRect());
                            prof.Stop();
                        }
                    }
                }
                __state = "Finalize mesh";
                prof = ProfileController.Start(__state, null, null, __originalMethod);
                __instance.FinalizeMesh(MeshParts.All);
                prof.Stop();
                return false;
            }

            return true;
        }
    }
}