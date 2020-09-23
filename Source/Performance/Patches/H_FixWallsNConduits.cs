using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Analyzer.Performance
{

    internal class H_FixWallsNConduits : PerfPatch
    {
        public static bool Enabled = false;
        public override string Name => "performance.wallsnconduits";

        public static void Swapclasses()
        {
            if (Enabled)
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

            if (Find.Maps == null) return;

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

                if (Enabled)
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

        public override void OnEnabled(Harmony harmony)
        {
            Swapclasses();
        }
    }
}