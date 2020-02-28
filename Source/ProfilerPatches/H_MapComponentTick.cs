using System;
using HarmonyLib;
using Verse;

namespace DubsAnalyzer
{
    [ProfileMode("MapComponentTick", UpdateMode.Tick)]
    [HarmonyPatch(typeof(MapComponentUtility), nameof(MapComponentUtility.MapComponentTick))]
    internal class H_MapComponentTick
    {
        public static bool Active = false;

        private static bool Prefix(Map map)
        {
            if (!Active)
            {
                return true;
            }

            var components = map.components;
            var c = components.Count;
            for (var i = 0; i < c; i++)
            {
                try
                {
                    var comp = components[i];

                    Analyzer.Start(comp.GetType().FullName, () => $"{comp.GetType()} MapCompTick");
                    comp.MapComponentTick();
                    Analyzer.Stop(comp.GetType().FullName);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }

            return false;
        }
    }
}