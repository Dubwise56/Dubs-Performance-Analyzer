using System;
using Analyzer.Profiling;
using UnityEngine;
using UnityEngine.Scripting;
using Verse;

namespace Analyzer.GCNotify
{
    class GarbageMan
    {
        public static bool GarbageRemains = false;
        public static long CurrentAllocatedMemory;
        public static long LastMinGC;
        public static long LastMaxGC;
        public static long totalBytesOfMemoryUsed;
        public static float delta;

        public static void Track()
        {
            long jam = totalBytesOfMemoryUsed;
            totalBytesOfMemoryUsed = GC.GetTotalMemory(false);

            if (jam > totalBytesOfMemoryUsed)
            {
                LastMinGC = totalBytesOfMemoryUsed;
                LastMaxGC = jam;
                //       GarbageCollectionInfo = $"{totalBytesOfMemoryUsed.ToMb()}MB";
            }

            delta += Time.deltaTime;
            if (delta >= 1f)
            {
                delta -= 1f;

                long PreviouslyAllocatedMemory = CurrentAllocatedMemory;
                CurrentAllocatedMemory = GC.GetTotalMemory(false);

                long MemoryDifference = CurrentAllocatedMemory - PreviouslyAllocatedMemory;
                if (MemoryDifference < 0)
                    MemoryDifference = 0;

                //     GarbageCollectionInfo = $"{CurrentAllocatedMemory.ToMb():0.00}MB +{MemoryDifference.ToMb():0.00}MB/s";
            }
        }

        public static void Init()
        {
            if (GarbageCollector.isIncremental)
            {
                Log.Warning($"Running incremental garbage collection with time slice of {GarbageCollector.incrementalTimeSliceNanoseconds} nanoseconds");
            }

          //  GarbageCollector.incrementalTimeSliceNanoseconds = 3000000;
        }

        public static void Collect()
        {
            var before = GC.GetTotalMemory(false);
            if (GarbageCollector.CollectIncremental(1000000))
            {
                if (GarbageRemains)
                {
                   
                    var after = GC.GetTotalMemory(false);
                    Log.Warning($"collection complete - { (before - after)} Collected - {after.ToMb():0.00}MB in heap", true);
                }
                GarbageRemains = false;
                 //  Log.Warning("collection complete", true);
            }
            else
            {
                GarbageRemains = true;
             //   Log.Warning("garbage remains", true);
            }

           

          
        }
    }
}