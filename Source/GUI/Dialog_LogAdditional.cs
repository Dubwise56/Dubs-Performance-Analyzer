using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace DubsAnalyzer
{
    public static class Dialog_LogAdditional
    {
        public static Profiler currentProfiler = null;

        public static void DoWindowContents(Rect position)
        {
            currentProfiler = Analyzer.Profiles[Dialog_Analyzer.CurrentKey];


            Widgets.DrawMenuSection(position);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(position);

            DrawStatistics(listing);

            listing.End();
            
        }

        public static void DrawStatistics(Listing_Standard listing)
        {
            DubGUI.Heading(listing, "Statistics");

            if (!Analyzer.Profiles.ContainsKey(Dialog_Analyzer.CurrentKey)) return;

            if (!LogStats.IsActiveThread)
            {
                var s = new LogStats();
                s.GenerateStats(currentProfiler.History.times, currentProfiler.History.hits);
            }

            if (CurrentLogStats.stats == null)
                listing.Label("Loading Stats!");
            else
            {
                lock (CurrentLogStats.sync)
                {
                    DrawStatsPage(listing.GetRect(400f));
                }
            }
        }

        public static void DrawStatsPage(Rect rect)
        {
            LeftStats(rect.LeftPart(.50f));
            RightStats(rect.RightPart(.50f));
            rect.y += 100;
            Dialog_Graph.DoGraph(rect.ContractedBy(10f));
        }
        public static void LeftStats(Rect rect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect);

            

            listing.End();
        }
        public static void RightStats(Rect rect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect);



            listing.End();
        }
    }
}
