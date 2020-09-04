using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Analyzer
{
    [Entry("SectionLayer_Things", UpdateMode.Update)]
    [HarmonyPatch(typeof(SectionLayer_Things), "Regenerate")]
    internal class H_SectionLayer_Things
    {
        public static bool Active = false;

        public static bool Prefix(MethodBase __originalMethod, SectionLayer_Things __instance, ref string __state)
        {
            if (Active)
            {

                __instance.ClearSubMeshes(MeshParts.All);
                Profiler prof = null;
                foreach (IntVec3 intVec in __instance.section.CellRect)
                {
                    List<Thing> list = __instance.Map.thingGrid.ThingsListAt(intVec);
                    int count = list.Count;
                    for (int i = 0; i < count; i++)
                    {
                        Thing thing = list[i];

                        __state = "Flag check";
                        prof = Modbase.Start(__state, null, null, null, null, __originalMethod);
                        bool flag =
                            ((thing.def.seeThroughFog ||
                              !__instance.Map.fogGrid.fogGrid[
                                  CellIndicesUtility.CellToIndex(thing.Position, __instance.Map.Size.x)]) &&
                             thing.def.drawerType != DrawerType.None &&
                             (thing.def.drawerType != DrawerType.RealtimeOnly || !__instance.requireAddToMapMesh) &&
                             (thing.def.hideAtSnowDepth >= 1f || __instance.Map.snowGrid.GetDepth(thing.Position) <=
                                 thing.def.hideAtSnowDepth) && thing.Position.x == intVec.x &&
                             thing.Position.z == intVec.z);
                        prof.Stop();

                        if (flag)
                        {
                            __state = thing.def.defName;
                            prof = Modbase.Start(__state, null, null, null, null, __originalMethod);
                            __instance.TakePrintFrom(thing);
                            prof.Stop();
                        }

                    }
                }

                __state = "Finalize mesh";
                prof = Modbase.Start(__state, null, null, null, null, __originalMethod);
                __instance.FinalizeMesh(MeshParts.All);
                prof.Stop();
                return false;
            }

            return true;
        }

    }
}