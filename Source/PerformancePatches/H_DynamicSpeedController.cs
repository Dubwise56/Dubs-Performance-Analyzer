using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace DubsAnalyzer
{
    [HarmonyPatch(typeof(TickManager))]
    [HarmonyPatch(nameof(TickManager.TickRateMultiplier), MethodType.Getter)]
    public static class Patch_TickRateMultiplier
    {
        public static float panNorm = 0.0f;
        public static float nMinusCameraScale = 0.0f;

        [HarmonyPriority(Priority.LowerThanNormal)]
        static void Postfix(ref float __result)
        {

            if (!Analyzer.Settings.DynamicSpeedControl)
            {
                return;
            }

            if (Find.CameraDriver.CurrentViewRect.Area != nMinusCameraScale)
            {
                nMinusCameraScale = Find.CameraDriver.CurrentViewRect.Area;

                if (Find.CameraDriver.CurrentZoom == CameraZoomRange.Furthest)
                    panNorm += 150f;

                if (Find.CameraDriver.CurrentZoom == CameraZoomRange.Far)
                    panNorm += 80f;

                if (Find.CameraDriver.CurrentZoom == CameraZoomRange.Middle)
                    panNorm += 40f;

                if (Find.CameraDriver.CurrentZoom == CameraZoomRange.Close)
                    panNorm += 5f;
            }

            if (panNorm <= 15f)
                return;

            if (Find.TickManager.CurTimeSpeed == TimeSpeed.Fast)
            {
                __result = Mathf.Max(2.0f - panNorm / 128f, 1.0f);
            }
            else if (Find.TickManager.CurTimeSpeed == TimeSpeed.Superfast)
            {
                __result = Mathf.Max(3.0f - panNorm / 96f, 1.5f);
            }
            else if (Find.TickManager.CurTimeSpeed == TimeSpeed.Ultrafast)
            {
                __result = Mathf.Max(4.0f - panNorm / 64f, 2.0f);
            }
        }
    }

    [HarmonyPatch(typeof(CameraDriver))]
    [HarmonyPatch("CalculateCurInputDollyVect")]
    static class Patch_CalculateCurnputDollyVect
    {
        static void Postfix(ref Vector2 __result)
        {
            Patch_TickRateMultiplier.panNorm = __result.magnitude;
        }
    }

    [HarmonyPatch(typeof(CameraDriver))]
    [HarmonyPatch(nameof(CameraDriver.CameraDriverOnGUI))]
    public static class Patch_CameraPositionChanged
    {
        [HarmonyPriority(Priority.HigherThanNormal)]
        static void Postfix(CameraDriver __instance)
        {
            if (!Analyzer.Settings.DynamicSpeedControl)
            {
                return;
            }

            if (Find.Camera == null)
                return;

            if (Find.TickManager.Paused || Find.TickManager.NotPlaying)
                return;

            var modifer = 0f;

            if (Find.CameraDriver.CurrentZoom == CameraZoomRange.Furthest)
                modifer = 150f;

            if (Find.CameraDriver.CurrentZoom == CameraZoomRange.Far)
                modifer = 80f;

            if (Find.CameraDriver.CurrentZoom == CameraZoomRange.Middle)
                modifer = 40f;

            if (Find.CameraDriver.CurrentZoom == CameraZoomRange.Close)
                modifer = 5f;

            if (KeyBindingDefOf.MapDolly_Left.IsDown || KeyBindingDefOf.MapDolly_Up.IsDown || KeyBindingDefOf.MapDolly_Right.IsDown || KeyBindingDefOf.MapDolly_Down.IsDown)
            {
                Patch_TickRateMultiplier.panNorm = modifer;
            }

            if (KeyBindingDefOf.MapZoom_Out.KeyDownEvent || KeyBindingDefOf.MapZoom_In.KeyDownEvent)
            {
                Patch_TickRateMultiplier.panNorm = modifer;
            }
        }
    }
}
