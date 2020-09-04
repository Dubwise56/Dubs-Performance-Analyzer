using System;
using System.ComponentModel;
using System.Reflection;
using Verse;

namespace Analyzer
{
    public struct ProfileLog
    {
        public string Average_s;
        public double Average;
        public string Key;
        public string Label;
        public string Mod;
        public float Max;
        public float Percent;
        public Type Type;
        public Def Def;
        public MethodBase Meth;

        public ProfileLog(string label, string averageS, double average, float max, Def def, string key, string mod, float percent, Type type, MethodBase meth = null)
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
            Meth = meth;
        }
    }
}