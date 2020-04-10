using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DubsAnalyzer
{
    [PerformancePatch]
    [ProfileMode("Frame times", UpdateMode.Update, "frameTimeProfilerTip", true)]
    [HarmonyPatch(typeof(Root), nameof(Root.Update))]
    public class H_RootUpdate
    {
        public static bool Active = false;
        public static string _fpsText;
        public static float _hudRefreshRate = 1f;
        public static float _timer;
        public static string tps;
        private static DateTime PrevTime;
        private static int PrevTicks;
        private static int TPSActual;
        public static float delta;
        public static long memrise = 0;
        public static long LastMinGC = 0;
        public static long LastMaxGC = 0;
        public static void Prefix()
        {
            if (Analyzer.running)
            {
                var jam = Dialog_Analyzer.totalBytesOfMemoryUsed;
                Dialog_Analyzer.totalBytesOfMemoryUsed = GC.GetTotalMemory(false);

                if (jam > Dialog_Analyzer.totalBytesOfMemoryUsed)
                {
                    LastMinGC = Dialog_Analyzer.totalBytesOfMemoryUsed;
                    LastMaxGC = jam;
                    Dialog_Analyzer.stlank = $"{Dialog_Analyzer.totalBytesOfMemoryUsed.ToMb()}MB";
                    //if (Analyzer.running && !Analyzer.Settings.MuteGC)
                    //    Messages.Message($"GC at {jam.ToMb()} MB", MessageTypeDefOf.TaskCompletion, false);
                }

                delta += Time.deltaTime;
                if (delta >= 1f)
                {
                    delta -= 1f;
                    var compare = memrise;
                    memrise = GC.GetTotalMemory(false);
                    var vtec = memrise - compare;
                    if (vtec < 0)
                    {
                        vtec = 0;
                    }
                    Dialog_Analyzer.stlank = $"{memrise.ToMb():0.00}MB +{vtec.ToMb():0.00}MB/s";
                }


                if (Time.unscaledTime > _timer)
                {
                    int fps = (int)(1f / Time.unscaledDeltaTime);
                    _fpsText = "FPS: " + fps;
                    _timer = Time.unscaledTime + _hudRefreshRate;
                }

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
                    }
                }

                tps = $"TPS: {TPSActual}({TPSTarget})";
            }


            if (Active)
            {
                Analyzer.Start("Game Update");
            }
        }

        public static void Postfix()
        {
            if (Active)
            {
                Analyzer.Stop("Game Update");
                Analyzer.Stop("Frame times");
            }

            if (Analyzer.SelectedMode != null)
            {
                if (Analyzer.SelectedMode.mode == UpdateMode.Update || Analyzer.SelectedMode.mode == UpdateMode.GUI)
                    Analyzer.UpdateEnd();
            }

            if (Active)
            {
                Analyzer.Start("Frame times");
            }
        }
    }
}