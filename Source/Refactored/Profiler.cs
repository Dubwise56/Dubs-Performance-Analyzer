using Analyzer;
using System;
using System.Diagnostics;
using System.Reflection;
using Verse;

namespace Analyzer
{
    public class Profiler
    {
        public const int MAX_ADD_INFO_PER_FRAME = 250;
        public const int RECORDS_HELD = 2000;

        private readonly Stopwatch stopwatch;
        public Type type;
        public Def def;
        public Thing thing;
        public MethodBase meth;

        public string label;
        public string key;

        public int hitCounter = 0;

        public readonly double[] times;
        public readonly int[] hits;
        public int currentIndex = 0; // ring buffer tracking

        public Profiler(string key, string label, Type type, Def def, Thing thing, MethodBase meth)
        {
            this.key = key;
            this.thing = thing;
            this.def = def;
            this.meth = meth;
            this.label = label;
            this.stopwatch = new Stopwatch();
            this.type = type;
            this.times = new double[RECORDS_HELD];
            this.hits = new int[RECORDS_HELD];
        }

        public Profiler Start()
        {
            stopwatch.Start();
            return this;
        }

        public void Stop()
        {
            if (Analyzer.CurrentlyPaused) return;

            try
            {
                stopwatch.Stop();
                hitCounter++;
            }
            catch (Exception e)
            {
                Log.Warning($"[Analyzer] Profile:Stop() failed with the error {e.Message}");
            }
        }

        // This function will be added via transpiler to the end of `Stop()` when the option is toggled.
        public static void StopFrameInfo(Profiler prof) 
        {
            if (prof == GUIController.GetCurrentProfile())
                if (prof.hitCounter < MAX_ADD_INFO_PER_FRAME)
                    StackTraceRegex.Add(new StackTrace(2, false));
        }

        public void RecordMeasurement()
        {
            if (stopwatch.IsRunning) Log.Error($"[Analyzer] Profile {key} was still running when recorded");

            times[currentIndex] = stopwatch.Elapsed.TotalMilliseconds;
            hits[currentIndex] = hitCounter;

            currentIndex++;
            currentIndex %= RECORDS_HELD; // ring buffer

            stopwatch.Stop();
            stopwatch.Reset();
            hitCounter = 0;
        }

        public double GetAverageTime(int entries)
        {
            // we traverse backwards through the array, so when we reach -1
            // we wrap around back to the end
            int arrayIndex = currentIndex;
            int i = entries;
            double sum = 0;

            while(i-- > 0)
            {
                sum += times[arrayIndex];
                arrayIndex--;
                arrayIndex %= RECORDS_HELD;
            }

            return sum/(float)entries;
        }
    }
}