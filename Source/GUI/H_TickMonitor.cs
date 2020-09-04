using HarmonyLib;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace Analyzer
{
    public static class H_TickMonitor
    {
        private static DateTime PrevTime;
        private static int PrevTicks;
        private static int TPSActual = 0;
        private static int PrevFrames;
        private static int FPSActual = 0;

        public static void PerformancePatch(Harmony harmony)
        {
            System.Reflection.MethodInfo jiff = AccessTools.Method(typeof(GlobalControlsUtility), nameof(GlobalControlsUtility.DoTimespeedControls));
            HarmonyMethod pre = new HarmonyMethod(typeof(H_TickMonitor), nameof(Prefix));
            harmony.Patch(jiff, pre);
        }

        public static void Prefix(float leftX, float width, ref float curBaseY)
        {

            float TRM = Find.TickManager.TickRateMultiplier;
            int TPSTarget = (int)Math.Round((TRM == 0f) ? 0f : (60f * TRM));

            if (PrevTicks == -1)
            {
                PrevTicks = GenTicks.TicksAbs;
                PrevTime = DateTime.Now;
            }
            else
            {
                DateTime CurrTime = DateTime.Now;
                if (CurrTime.Second != PrevTime.Second)
                {
                    PrevTime = CurrTime;
                    TPSActual = GenTicks.TicksAbs - PrevTicks;
                    PrevTicks = GenTicks.TicksAbs;
                    FPSActual = PrevFrames;
                    PrevFrames = 0;
                }
            }
            PrevFrames++;

            Rect rect = new Rect(leftX - 20f, curBaseY - 26f, width + 20f - 7f, 26f);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(rect, "TPS: " + TPSActual.ToString() + "(" + TPSTarget.ToString() + ")");
            rect.y -= 26f;
            Widgets.Label(rect, "FPS: " + FPSActual.ToString());
            curBaseY -= 52f;
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}
