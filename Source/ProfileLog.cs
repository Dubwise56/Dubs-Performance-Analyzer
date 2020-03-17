using System;
using System.ComponentModel;
using Verse;

namespace DubsAnalyzer
{
    [ImmutableObject(true)]
    public readonly struct ProfileLog
    {
        public readonly string Average_s;
        public readonly double Average;
        public readonly string Key;
        public readonly string Label;
        public readonly string Mod;
        public readonly float Max;
        public readonly float Percent;
        public readonly Type Type;
        public readonly Def Def;

        public ProfileLog(string label, string averageS, double average, float max, Def def, string key, string mod, float percent, Type type)
        {
            Label = label;
            Average_s = averageS;
            Average = average;
            Def = def;
            Key = key;
            Max = max;
            Mod = mod;
            Percent = percent;
            Type = type;
        }
    }
}