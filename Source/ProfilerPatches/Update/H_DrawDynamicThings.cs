using System;
using HarmonyLib;
using DubsAnalyzer;
using Verse;
using System.Reflection;

namespace DubsAnalyzer
{


    [ProfileMode("DrawDynamicThings", UpdateMode.Update, "DrawDynThinTipKey", true)]
    [HarmonyPatch(typeof(DynamicDrawManager), nameof(DynamicDrawManager.DrawDynamicThings))]
    internal class H_DrawDynamicThings
    {

        [Setting("By Def")]
        public static bool ByDef=false;

        public static bool Active=false;
        public static bool Prefix(MethodBase __originalMethod, DynamicDrawManager __instance)
        {
            if (!Active)
            {
                return true;
            }

            __instance.drawingNow = true;
            try
            {
                var fogGrid = __instance.map.fogGrid.fogGrid;
                var cellRect = Find.CameraDriver.CurrentViewRect;
                cellRect.ClipInsideMap(__instance.map);
                cellRect = cellRect.ExpandedBy(1);
                var cellIndices = __instance.map.cellIndices;
                foreach (var thing in __instance.drawThings)
                {
                    var position = thing.Position;
                    if (cellRect.Contains(position) || thing.def.drawOffscreen)
                    {
                        if (!fogGrid[cellIndices.CellToIndex(position)] || thing.def.seeThroughFog)
                        {
                            if (thing.def.hideAtSnowDepth >= 1f || __instance.map.snowGrid.GetDepth(thing.Position) <=
                                thing.def.hideAtSnowDepth)
                            {
                                try
                                {
                                    string Namer()
                                    {
                                        var n = string.Empty;
                                        if (ByDef)
                                        {
                                            n = $"{thing.def.defName} - {thing?.def?.modContentPack?.Name}";
                                        }
                                        else
                                        {
                                            n = thing.GetType().ToString();
                                        }

                                        return n;
                                    }

                                    var name = string.Empty;
                                    if (ByDef)
                                    {
                                        name = thing.def.defName;
                                    }
                                    else
                                    {
                                        name = thing.GetType().Name;
                                    }

                                    Analyzer.Start(name, Namer, thing.GetType(), thing.def, null, __originalMethod);
                                    thing.Draw();
                                    Analyzer.Stop(name);
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(string.Concat("Exception drawing ", thing, ": ", ex.ToString()));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception arg)
            {
                Log.Error("Exception drawing dynamic things: " + arg);
            }

            __instance.drawingNow = false;
            return false;
        }
    }
}