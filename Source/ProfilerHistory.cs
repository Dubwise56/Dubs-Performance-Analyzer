using System.Linq;
using UnityEngine;
using Verse;

namespace DubsAnalyzer
{
    public readonly struct Stack
    {
        public readonly string[] stacks;

        public Stack(string[] stacks)
        {
            this.stacks = stacks;
        }
    }

    public readonly struct ProfilerHistory
    {
        public readonly double[] times;
      //  public readonly long[] mem;
      //  public readonly Stack[] stack;
        public readonly int[] hits;

        public ProfilerHistory(int maxEntries)
        {
            times = new double[2000];
       //     mem = new long[2000];
         //   stack = new Stack[2000];
            hits = new int[2000];
        }

        public void AddMeasurement(double measurement, int HitCounter)
        {
            for (var i = times.Length - 1; i >= 0; i--)
            {
                if (i == 0)
                {
                    times[0] = measurement;
                   // mem[0] = bytes;
                  //  stack[0] = new Stack(stacks);
                    hits[0] = HitCounter;
                }
                else
                {
                    times[i] = times[i - 1];
               //     mem[i] = mem[i - 1];
                  //  stack[i] = stack[i - 1];
                    hits[i] = hits[i - 1];
                }
            }
        }

        public double GetAverageTime(int count)
        {
            return times.Take(count).Average(timeSpan => timeSpan);
            // var longAverageTicks = Convert.ToInt64(av);
            //  return new TimeSpan(longAverageTicks);
        }

        //public double GetAverageBytes(int count)
        //{
        //    return mem.Take(count).Average();
        //}
    }
}