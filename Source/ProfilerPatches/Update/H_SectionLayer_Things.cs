using System.Collections.Generic;
using System.Reflection;
using System.Security.Policy;
using HarmonyLib;
using Verse;

namespace DubsAnalyzer
{
    [ProfileMode("SectionLayer_Things", UpdateMode.Update)]
    [HarmonyPatch(typeof(SectionLayer_Things), "Regenerate")]
    internal class H_SectionLayer_Things
    {
        public static bool Active = false;
        
        public static bool Prefix(MethodBase __originalMethod, SectionLayer_Things __instance, ref string __state)
        {
            if (Active)
            {

                __instance.ClearSubMeshes(MeshParts.All);
                foreach (IntVec3 intVec in __instance.section.CellRect)
                {
                    List<Thing> list = __instance.Map.thingGrid.ThingsListAt(intVec);
                    int count = list.Count;
                    for (int i = 0; i < count; i++)
                    {
                        Thing thing = list[i];

                        __state = "Flag check";
                        Analyzer.Start(__state, null, null, null, null, __originalMethod as MethodInfo);
                        var flag =
                            ((thing.def.seeThroughFog ||
                              !__instance.Map.fogGrid.fogGrid[
                                  CellIndicesUtility.CellToIndex(thing.Position, __instance.Map.Size.x)]) &&
                             thing.def.drawerType != DrawerType.None &&
                             (thing.def.drawerType != DrawerType.RealtimeOnly || !__instance.requireAddToMapMesh) &&
                             (thing.def.hideAtSnowDepth >= 1f || __instance.Map.snowGrid.GetDepth(thing.Position) <=
                                 thing.def.hideAtSnowDepth) && thing.Position.x == intVec.x &&
                             thing.Position.z == intVec.z);
                        Analyzer.Stop(__state);

                        if (flag)
                        {
                            __state = thing.def.defName;
                            Analyzer.Start(__state, null, null, null, null, __originalMethod as MethodInfo);
                            __instance.TakePrintFrom(thing);
                            Analyzer.Stop(__state);
                        }

                    }
                }

                __state = "Finalize mesh";
                Analyzer.Start(__state, null, null, null, null, __originalMethod as MethodInfo);
                __instance.FinalizeMesh(MeshParts.All);
                Analyzer.Stop(__state);
                return false;
            }

            return true;
        }

    }
}