using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DubsAnalyzer
{
    [HarmonyPatch(typeof(Root), nameof(Root.Update))]
    public class H_RootUpdate
    {
        public static float delta;
        public static long memrise = 0;
        public static long LastMinGC = 0;
        public static long LastMaxGC = 0;
        public static void Prefix()
        {
            if (Analyzer.Settings.AdvancedMode && (Analyzer.running || Analyzer.Settings.ShowOnMainTab))
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
            }
        }

        public static void Postfix()
        {
            if (Analyzer.SelectedMode != null)
            {
                if (Analyzer.SelectedMode.mode == UpdateMode.Update || Analyzer.SelectedMode.mode == UpdateMode.GUI)
                    Analyzer.UpdateEnd();
            }
        }
    }
}