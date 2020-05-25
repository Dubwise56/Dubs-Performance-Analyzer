using System;
using HarmonyLib;
using Verse;

namespace DubsAnalyzer
{
    [ProfileMode("Sections", UpdateMode.Update)]
    [HarmonyPatch(typeof(Section), nameof(Section.RegenerateLayers))]
    internal class H_RegenerateLayers
    {
        public static bool Active = false;
        public static bool Prefix(Section __instance, MapMeshFlag changeType, ref string __state)
        {
            if (Active)
            {
                for (int i = 0; i < __instance.layers.Count; i++)
                {
                    SectionLayer sectionLayer = __instance.layers[i];
                    if ((sectionLayer.relevantChangeTypes & changeType) != MapMeshFlag.None)
                    {
                        try
                        {
                            __state = __instance.layers[i].GetType().FullName;
                            Analyzer.Start(__state);
                            sectionLayer.Regenerate();
                            Analyzer.Stop(__state);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(string.Concat(new object[]
                            {
                                "Could not regenerate layer ",
                                sectionLayer.ToStringSafe<SectionLayer>(),
                                ": ",
                                ex
                            }), false);
                        }
                    }
                }

                return false;
            }

            return true;
        }

        //public static void Postfix(string __state)
        //{
        //    if (Active)
        //    {
        //        Analyzer.Stop(__state);
        //    }
        //}
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