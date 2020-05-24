using HarmonyLib;
using UnityEngine;
using Verse;

namespace DubsAnalyzer
{
    //[PerformancePatch]
    //internal class H_DrawFieldEdges
    //{
    //    public static void PerformancePatch(Harmony harmony)
    //    {
    //        var biff = new HarmonyMethod(typeof(H_DrawFieldEdges), nameof(Prefix));
    //        var skiff = AccessTools.Method(typeof(Room), nameof(Room.DrawFieldEdges));
    //        Analyzer.harmony.Patch(skiff, biff);
    //    }

    //    public static bool Prefix(Room __instance)
    //    {
    //        if (Analyzer.Settings.FixBedMemLeak)
    //        {
    //            Room.fields.Clear();
    //            Room.fields.AddRange(__instance.Cells);
    //            Color color = __instance.isPrisonCell ? Room.PrisonFieldColor : Room.NonPrisonFieldColor;
    //           // color.a = Pulser.PulseBrightness(1f, 0.6f);
    //            GenDraw.DrawFieldEdges(Room.fields, color);
    //            Room.fields.Clear();
    //            Log.Warning("mat count +" + MaterialPool.matDictionary.Count, true);
    //            return false;
    //        }
    //        return true;
    //    }
    //}
}