using System;
using System.ComponentModel;
using System.Reflection;
using Verse;

namespace Analyzer.Profiling
{
    public class ProfileLog
    {
        public int entries;
        public float percent;
        public double average;
        public string key;
        public string label;
        public float max;
        public float total;
        public float calls;
        public Type type;
        public MethodBase meth;
        public bool pinned;

        public ProfileLog(int entries, string label, double average, float max, string key, float total, float calls, float maxCalls, Type type, MethodBase meth = null, bool pinned = false)
        {
            this.entries = entries;
            this.label = label;
            this.average = average;
            this.key = key;
            this.max = max;
            this.type = type;
            this.meth = meth;
            this.total = total;
            this.calls = calls;
            this.pinned = pinned;
        }
    }
}