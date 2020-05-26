using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace DubsAnalyzer
{

    [PerformancePatch]
    [ProfileMode("Frame times", UpdateMode.Update, "frameTimeProfilerTip", true)]
    [HarmonyPatch(typeof(Root_Play), nameof(Root_Play.Update))]
    public class H_RootUpdate
    {
        public static bool Active = false;
        public static string _fpsText;
        public static float _hudRefreshRate = 1f;
        public static float _timer;
        public static string tps;
        private static DateTime PrevTime;
        private static int PrevTicks;
        public static int TPSActual;
        public static float delta;

        public static long CurrentAllocatedMemory;
        public static long LastMinGC;
        public static long LastMaxGC;

        public static void Prefix()
        {
            if (AnalyzerState.CurrentlyRunning)
            {
                long jam = Dialog_Analyzer.totalBytesOfMemoryUsed;
                Dialog_Analyzer.totalBytesOfMemoryUsed = GC.GetTotalMemory(false);

                if (jam > Dialog_Analyzer.totalBytesOfMemoryUsed)
                {
                    LastMinGC = Dialog_Analyzer.totalBytesOfMemoryUsed;
                    LastMaxGC = jam;
                    Dialog_Analyzer.GarbageCollectionInfo = $"{Dialog_Analyzer.totalBytesOfMemoryUsed.ToMb()}MB";
                    //if (Analyzer.running && !Analyzer.Settings.MuteGC)
                    //    Messages.Message($"GC at {jam.ToMb()} MB", MessageTypeDefOf.TaskCompletion, false);
                }

                delta += Time.deltaTime;
                if (delta >= 1f)
                {
                    delta -= 1f;

                    var PreviouslyAllocatedMemory = CurrentAllocatedMemory;
                    CurrentAllocatedMemory = GC.GetTotalMemory(false);

                    var MemoryDifference = CurrentAllocatedMemory - PreviouslyAllocatedMemory;
                    if (MemoryDifference < 0)
                        MemoryDifference = 0;

                    Dialog_Analyzer.GarbageCollectionInfo = $"{CurrentAllocatedMemory.ToMb():0.00}MB +{MemoryDifference.ToMb():0.00}MB/s";
                }


                if (Time.unscaledTime > _timer) 
                {
                    int fps = (int)(1f / Time.unscaledDeltaTime);
                    _fpsText = "FPS: " + fps;
                    _timer = Time.unscaledTime + _hudRefreshRate; // Every second we update our fps
                }

                try
                {
                    if (Current.ProgramState == ProgramState.Playing)
                    {
                        var TRM = Find.TickManager.TickRateMultiplier;
                        var TPSTarget = (int)Math.Round(TRM == 0f ? 0f : 60f * TRM);

                        var CurrTime = DateTime.Now;

                        if (CurrTime.Second != PrevTime.Second)
                        {
                            PrevTime = CurrTime;
                            TPSActual = GenTicks.TicksAbs - PrevTicks;
                            PrevTicks = GenTicks.TicksAbs;
                        }

                        tps = $"TPS: {TPSActual}({TPSTarget})";
                    }
                }
                catch (Exception) { }
            }

            if (Active)
                Analyzer.Start("Game Update");

        }

        public static void Postfix()
        {
            if (Active)
            {
                Analyzer.Stop("Frame times");
                Analyzer.Stop("Game Update");
            }

            if (AnalyzerState.CurrentTab?.mode == UpdateMode.Update || AnalyzerState.CurrentTab?.mode == UpdateMode.GUI)
                Analyzer.UpdateEnd();

            if (Active)
                Analyzer.Start("Frame times");

        }
    }
}