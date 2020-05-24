using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DubsAnalyzer
{
    [PerformancePatch]
    internal class H_FixWallsNConduits
    {
        public static void Swapclasses()
        {
            if (Analyzer.Settings.MeshOnlyBuildings)
            {
                DefDatabase<ThingDef>.GetNamed("PowerConduit").drawerType = DrawerType.MapMeshOnly;
                DefDatabase<ThingDef>.GetNamed("WaterproofConduit").drawerType = DrawerType.MapMeshOnly;
                DefDatabase<ThingDef>.GetNamed("Wall").drawerType = DrawerType.MapMeshOnly;
            }
            else
            {
                DefDatabase<ThingDef>.GetNamed("PowerConduit").drawerType = DrawerType.MapMeshAndRealTime;
                DefDatabase<ThingDef>.GetNamed("WaterproofConduit").drawerType = DrawerType.MapMeshAndRealTime;
                DefDatabase<ThingDef>.GetNamed("Wall").drawerType = DrawerType.MapMeshAndRealTime;
            }

            if (Find.Maps == null)
            {
                return;
            }
            foreach (var map in Find.Maps)
            {
                void reg(ThingDef d)
                {
                    if (map.listerThings.listsByDef.ContainsKey(d))
                    {
                        foreach (var def in map.listerThings.listsByDef[d])
                        {
                            map.dynamicDrawManager.RegisterDrawable(def);
                        }
                    }
                }

                void dereg(ThingDef d)
                {
                    if (map.listerThings.listsByDef.ContainsKey(d))
                    {
                        foreach (var def in map.listerThings.listsByDef[d])
                        {
                            map.dynamicDrawManager.DeRegisterDrawable(def);
                        }
                    }
                }

                if (Analyzer.Settings.MeshOnlyBuildings)
                {
                    dereg(DefDatabase<ThingDef>.GetNamed("PowerConduit"));
                    dereg(DefDatabase<ThingDef>.GetNamed("WaterproofConduit"));
                    dereg(DefDatabase<ThingDef>.GetNamed("Wall"));
                }
                else
                {
                    reg(DefDatabase<ThingDef>.GetNamed("PowerConduit"));
                    reg(DefDatabase<ThingDef>.GetNamed("WaterproofConduit"));
                    reg(DefDatabase<ThingDef>.GetNamed("Wall"));
                }
            }
        }

        public static void PerformancePatch(Harmony harmony)
        {
            Swapclasses();
        }
    }
}