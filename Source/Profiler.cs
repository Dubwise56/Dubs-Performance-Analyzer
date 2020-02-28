using System;
using System.Diagnostics;
using System.Linq;
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
        private long startBytes = 0;
        private long endBytes = 0;
        public long BytesUsed = 0;
        public string label;
        public string key;

        public long memRise = 0;
        public long LastBytesRecorded = 0;
        public string memRiseStr;

        public double lastTime = 0;
        public double startTime = 0;

        public int HitCounter = 0;

        public string[] fullstack = new string[20000];

        public Profiler(string kley , string lab, Type ty, Def indef, Thing inthing)
        {
            key = kley;
            thing = inthing;
            def = indef;
            label = lab;
            stopwatch = new Stopwatch();
            type = ty;
            startBytes = 0;
            endBytes = 0;
            BytesUsed = 0;
            History = new ProfilerHistory(Analyzer.MaxHistoryEntries);
        }

        public void Start()
        {
           // startBytes = GC.GetTotalMemory(false);
          //  startTime = 0;
            stopwatch.Start();
        }

        public void MemRiseUpdate()
        {
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
        }

        public void Stop(bool writestack)
        {
            stopwatch.Stop();

            //endBytes = GC.GetTotalMemory(false);
            //var g = endBytes - startBytes;
            //if (g > 0)
            //{
            //    BytesUsed += g;
            //}

            // lastTime = stopwatch.Elapsed.TotalMilliseconds;
            //  var t = lastTime - startTime;

            if (writestack)
            {
                Log.Warning(label);
              //  fullstack[HitCounter] = StackTraceUtility.ExtractStackTrace();
            }



            //if (stopwatch.Elapsed.TotalMilliseconds > 5.0)
            //{
            //    Log.Warning(label);
            //}


            HitCounter++;
        }


        public void RecordMeasurement()
        {
            History.AddMeasurement(stopwatch.Elapsed.TotalMilliseconds, BytesUsed, HitCounter);
            if (stopwatch.IsRunning)
            {
                Log.Error($"{key} was still running when recorded");
            }

            stopwatch.Stop();
            stopwatch.Reset();
            HitCounter = 0;
            lastTime = 0;
        }
    }
}