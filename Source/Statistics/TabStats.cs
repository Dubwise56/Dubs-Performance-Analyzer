using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace DubsAnalyzer
{
    public static class CurrentTabStats // 'Current' stats that our drawing will access
    {
        public static object sync = new object();
        public static TabStats stats = null;
    }

    public class TabStats
    {
        public Dictionary<string, LinkedQueue<float>> timePoints = new Dictionary<string, LinkedQueue<float>>();
        public LinkedQueue<Tuple<string, IntVec2[]>> verts = new LinkedQueue<Tuple<string, IntVec2[]>>(Vertices);
        public double CurrentSum = 0;
        public double Highest = 0;

        public static Thread thread = null;
        public static bool IsActiveThread = false;
        public static int Vertices => 30; // How many verticies on our graph  

        public int LogsPerGraph = 10; // How many, at max 'logs' do we display on our graph
        public int Entries = 300; // How many, ticks worth of entries to we display at once
        public bool clearOld = true;

        //
        public int Granularity => Entries/10; // we get a 'moving' average of the last Granularity values from our input.

        public void GenerateStats()
        {
            if (!IsActiveThread)
            {
                thread = new Thread(() => ExecuteWorker(this, AnalyzerState.CurrentProfiles.Values.ToList()));
                thread.Start();
            }
        }

        private static void ExecuteWorker(TabStats logic, List<Profiler> profiles)
        {
            IsActiveThread = true;

            var oldHighest = CurrentTabStats.stats?.Highest ?? 0;

            double[] sums = new double[profiles.Count];

            for (int i = 0; i < profiles.Count; i++)
            {
                double locSum = 0;
                for (int j = 0; j < logic.Granularity; j++)
                {
                    locSum += profiles[i].History.times[j];
                }

                // Add our averaged values // enter into dict if doesn't already exist

                if (logic.timePoints.ContainsKey(profiles[i].label))
                {
                    logic.timePoints[profiles[i].label].Enqueue((float)locSum / logic.Granularity);
                }
                else
                {
                    logic.timePoints.Add(profiles[i].label, new LinkedQueue<float>(Vertices));

                    logic.timePoints[profiles[i].label]
                        .Enqueue((float)locSum / logic.Granularity) 
                        .MaxValues = Vertices; // set the maximum number of values to our vertex count
                }

                sums[i] = locSum;
            }

            // get our top LogsPerGraph
            sums.SortStable(null);
            Log.Message(sums.Count().ToString());
            foreach(var thing in sums)
            {
                Log.Message(thing.ToString());
            }

            IsActiveThread = false;
        }
    }
}
