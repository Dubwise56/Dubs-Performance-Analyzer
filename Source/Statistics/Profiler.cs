using System;
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

        // static Dict<StackTrace, int>
        // static int[] mem

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
            stopwatch.Start();
        }

        public void Stop(bool writestack)
        {
            stopwatch.Stop();

            if (writestack)
                Log.Warning(label);

            HitCounter++;
        }


        public void RecordMeasurement()
        {
            double timeElapsed = stopwatch.Elapsed.TotalMilliseconds;
            History.AddMeasurement(timeElapsed, HitCounter);

            if (stopwatch.IsRunning)
            {
                Log.Error($"{key} was still running when recorded");
            }

            stopwatch.Stop();
            stopwatch.Reset();
            HitCounter = 0;
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