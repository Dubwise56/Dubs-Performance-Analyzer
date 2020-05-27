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
        public double CurrentSum = 0;
        public LinkedQueue<Tuple<string, IntVec2[]>> verts = new LinkedQueue<Tuple<string, IntVec2[]>>(Vertices);

        public static Thread thread = null;
        public static bool IsActiveThread = false;
        public static int Vertices => 30; // How many verticies on our graph  
        public static int LogsPerGraph = 10; // How many, at max 'logs' do we display on our graph
        public static int Entries = 300; // How many, ticks worth of entries to we display at once
        public static bool clearOld = true;

        public void GenerateStats()
        {
            thread = new Thread(() => ExecuteWorker(this, AnalyzerState.CurrentProfiles.Values.ToList()));
            thread.Start();
        }

        private static void ExecuteWorker(TabStats logic, List<Profiler> profiles)
        {
            IsActiveThread = true;

            logic.InitData();

            double sum = 0;
            for (int i = 0; i < profiles.Count; i++)
            {
                double locSum = 0;
                for (int j = 0; j < Vertices; j++)
                    locSum += profiles[i].History.times[j];

                // Add our averaged values -> enter into dict if doesn't already exist

                if (logic.timePoints.ContainsKey(profiles[i].label))
                {
                    logic.timePoints[profiles[i].label]
                        .Enqueue((float)locSum / Vertices);
                }
                else
                {
                    logic.timePoints.Add(profiles[i].label, new LinkedQueue<float>(Vertices));
                    logic.timePoints[profiles[i].label]
                        .Enqueue((float)locSum / Vertices) 
                        .MaxValues = Vertices; // set the maximum number of values to our vertex count
                }
                sum += locSum;
            }

            double[] sums = new double[Vertices];
            int counter = 0;
            foreach (var value in logic.timePoints.Values)// .AsParalell - to try
            {
                counter = 0;
                foreach (var val in value)
                {
                    sums[counter++] += val;
                }
            }

            // this is the value we now need to use as the 'highest' value in our graph, everything else needs a percentage value based upon this value!
            double largest = sums.Max(); 

            // get our top LogsPerGraph
            
            


            IsActiveThread = false;
        }

        private void InitData()
        {
            TabStats oldStats = null;
            if (!clearOld) // If we have swapped tabs, we don't want data from the old tab to persist
            {
                lock (CurrentTabStats.sync)
                {
                    oldStats = CurrentTabStats.stats;
                }
                timePoints = oldStats.timePoints;
                CurrentSum = oldStats.CurrentSum;
                verts = oldStats.verts;
            }
            else
            {
                clearOld = false;
            }
            // take the top off of our sum if req'd
            if (timePoints.First().Value.Count == timePoints.First().Value.MaxValues)
                foreach (var point in timePoints.Values)
                    CurrentSum -= point.Peak();
        }

        public static void reset()
        {
            clearOld = true;
        }
    }
}
