using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace DubsAnalyzer
{
    public class Profiler
    {
        public static int MAX_TRACES_PER_FRAME = 250;

        private Stopwatch stopwatch;
        public ProfilerHistory History;
        public Type type;
        public Def def;
        public Thing thing;

        public string label;
        public string key;

        public MethodInfo meth;

        public double lastTime = 0;
        public double startTime = 0;
        public int HitCounter = 0;

        public static double[] memDiffs = new double[2000];
        static double memChangeTot = 0;
        static double membefore = 0;

        public static void Reset()
        {
            memDiffs = new double[2000];
            membefore = 0;
            memChangeTot = 0;
        }

        public Profiler(string kley, string lab, Type ty, Def indef, Thing inthing, MethodInfo inmeth)
        {
            key = kley;
            thing = inthing;
            def = indef;
            meth = inmeth;
            label = lab;
            stopwatch = new Stopwatch();
            type = ty;
            History = new ProfilerHistory(Analyzer.MaxHistoryEntries);
        }

        public void Start()
        {
            if (key == AnalyzerState.CurrentProfileKey)
            {
                membefore = GC.GetTotalMemory(false);
            }

            stopwatch.Start();
        }

        public void Stop(bool writestack)
        {
            stopwatch.Stop();
            HitCounter++;

            if (key == AnalyzerState.CurrentProfileKey)
            {
                if (!AnalyzerState.HideStatistics)
                {
                    if(HitCounter < MAX_TRACES_PER_FRAME)
                        StackTraceRegex.Add(new System.Diagnostics.StackTrace(3, false));
                }

                memChangeTot += GC.GetTotalMemory(false) - membefore;
            }

            if (writestack)
                Log.Warning(label);

        }

        public void RecordMeasurement()
        {
            double timeElapsed = stopwatch.Elapsed.TotalMilliseconds;
            History.AddMeasurement(timeElapsed, HitCounter);

            if (key == AnalyzerState.CurrentProfileKey)
            {
                for (var i = 1999; i >= 0; i--)
                {
                    if (i == 0)
                        memDiffs[0] = memChangeTot / (float)HitCounter;
                    else
                        memDiffs[i] = memDiffs[i - 1];
                }
            }

            if (stopwatch.IsRunning)
            {
                Log.Error($"{key} was still running when recorded");
            }

            stopwatch.Stop();
            stopwatch.Reset();
            HitCounter = 0;
            memChangeTot = 0;
            lastTime = timeElapsed;
        }
    }


    //   public void MemRiseUpdate()
    //   {
    //var compare = LastBytesRecorded;
    //LastBytesRecorded = BytesUsed;
    //memRise = BytesUsed - compare;
    //if (memRise <= 0)
    //{
    //    memRise = 0;
    //    memRiseStr = string.Empty;
    //}
    //else
    //{
    //    memRiseStr = $"+{memRise / 1024}KB";
    //}
    //  }

    // private long startBytes = 0;
    // private long endBytes = 0;
    // public long BytesUsed = 0;

    //if (stopwatch.Elapsed.TotalMilliseconds > 5.0)
    //{
    //    Log.Warning(label);
    //}

    //endBytes = GC.GetTotalMemory(false);
    //var g = endBytes - startBytes;
    //if (g > 0)
    //{
    //    BytesUsed += g;
    //}

    // lastTime = stopwatch.Elapsed.TotalMilliseconds;
    //  var t = lastTime - startTime;

    // public long memRise = 0;
    // public long LastBytesRecorded = 0;
    // public string memRiseStr;

}