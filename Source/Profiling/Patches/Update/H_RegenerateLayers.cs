using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace Analyzer.Profiling
{
    [StaticConstructorOnStartup]
    internal class H_RegenerateLayers
    {
        public static Entry p = Entry.Create("Sections", Category.Update, "", typeof(H_RegenerateLayers), false);

        public static bool Active = false;

        public static IEnumerable<MethodInfo> GetPatchMethods() => typeof(SectionLayer).AllSubclasses().Select(sl => sl.GetMethod("Regenerate"));
        public static string GetLabel(SectionLayer __instance) => __instance.GetType().FullName;
    }

    // [ProfileMode("MapDrawer", UpdateMode.Update)]
    //[HarmonyPatch(typeof(MapDrawer), nameof(MapDrawer.MapMeshDrawerUpdate_First))]
    internal class H_MapMeshDrawerUpdate_First
    {
        public static bool Active = false;
        public static bool Prefix(MapDrawer __instance, ref string __state)
        {
            if (Active)
            {
                CellRect visibleSections = __instance.VisibleSections;
                bool flag = false;
                foreach (IntVec3 intVec in visibleSections)
                {
                    Section sect = __instance.sections[intVec.x, intVec.z];
                    if (__instance.TryUpdateSection(sect))
                    {
                        Log.Warning("drew a section on screen");
                        flag = true;
                    }
                }
                if (!flag)
                {
                    for (int i = 0; i < __instance.SectionCount.x; i++)
                    {
                        for (int j = 0; j < __instance.SectionCount.z; j++)
                        {
                            if (__instance.TryUpdateSection(__instance.sections[i, j]))
                            {
                                Log.Warning("full loop");
                                return false;
                            }
                        }
                    }
                }

                return false;
            }

            return true;
        }
    }


}