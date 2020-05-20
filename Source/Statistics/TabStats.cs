using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DubsAnalyzer
{
    public static class CurrentTabStats // 'Current' stats that our drawing will access
    {
        public static object sync = new object();
        public static LogStats stats = null;
    }
    public class TabStats
    {
        public float[] LocalTimes;

        public static Thread thread = null;
        public static bool IsActiveThread = false;
        public static int TicksPerEntry = 5;
        public static int LogsPerGraph = 10;

        public void GenerateStats()
        {
            thread = new Thread(() => ExecuteWorker(this, Analyzer.Profiles.Values.ToList()));
            thread.Start();
        }

        private static void ExecuteWorker(TabStats logic, List<Profiler> profiles)
        {
            IsActiveThread = true;


            IsActiveThread = false;
        }
    }
}
