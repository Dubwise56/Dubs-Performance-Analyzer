using HarmonyLib;
using RimWorld;
using System.Reflection;
using UnityEngine;
using Verse;

namespace DubsAnalyzer
{
    [ProfileMode("PawnRenderer", UpdateMode.Update)]
    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt), typeof(Vector3), typeof(RotDrawMode), typeof(bool), typeof(bool))]
    internal class H_RenderPawnAt
    {
        public static bool Active = false;

        //public static void PatchMe()
        //{
        //    var biff = new HarmonyMethod(typeof(H_RenderPawnAt).GetMethod(nameof(Prefix)), new[] { typeof(Vector3), typeof(RotDrawMode), typeof(bool), typeof(bool) }]);
        //    var skiff = typeof(PawnRenderer).GetMethod(nameof(PawnRenderer.RenderPawnAt));
        //    Analyzer.harmony.Patch(skiff, biff);
        //}

        [HarmonyPriority(Priority.Last)]
        public static void Prefix(MethodBase __originalMethod, PawnRenderer __instance, ref Profiler __state)
        {
            if (Active)
            {
                __state = Analyzer.Start(__instance.pawn.GetHashCode().ToString(), () => $"{__instance.pawn.Label} - {__instance.pawn.ThingID}", null, null, null, __originalMethod);
            }
        }

        [HarmonyPriority(Priority.First)]
        public static void Postfix(Profiler __state)
        {
            if (Active)
            {
                __state.Stop();
            }
        }
    }
}