using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace DubsAnalyzer
{
    // We hold reference to stats about the current 'wave' of statistics
    public struct WaveInfo
    {
        public WaveInfo(double d, int t)
        {
            this.delta = d;
            this.ticks = t;
        }
        public double delta;
        public int ticks;

        public void Log()
        {
            Verse.Log.Message($"Delta: {delta}, Ticks: {ticks} ");
        }
    }

    

    public static class Wave
    {
        private static int CurrentID = 0;
        private static List<WaveInfo> deltas = new List<WaveInfo>();
        public static long Increment(double deltaChange, int ticks)
        {
            deltas.Add(new WaveInfo(deltaChange, ticks));
            deltas[CurrentID].Log();
            return ++CurrentID;
        }

        public static int Current()
        {
            return CurrentID;
        }
        public static double GetDeltaAt(int index)
        {
            return deltas[index].delta;
        }

        public static void Reset()
        {
            CurrentID = 0;
            deltas.Clear();
        }
    }
}
