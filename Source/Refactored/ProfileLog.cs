using System;
using System.ComponentModel;
using System.Reflection;
using Verse;

namespace Analyzer
{
    public class ProfileLog
    {
        public string averageString;
        public double average;
        public string key;
        public string label;
        public string mod;
        public float max;
        public float percent;
        public Type type;
        public Def def;
        public MethodBase meth;

        public ProfileLog(string label, string averageString, double average, float max, Def def, string key, string mod, float percent, Type type, MethodBase meth = null)
        {
            this.label = label;
            this.averageString = averageString;
            this.average = average;
            this.def = def;
            this.key = key;
            this.max = max;
            this.mod = mod;
            this.percent = percent;
            this.type = type;
            this.meth = meth;
        }
    }
}