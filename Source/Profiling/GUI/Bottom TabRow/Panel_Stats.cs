using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    public static class Panel_Stats
    {
        public static void DrawStats(Rect inrect, GeneralInformation? currentInformation)
        {
            var stats = new LogStats();
            stats.GenerateStats();


            lock (CurrentLogStats.sync)
            {
                stats = CurrentLogStats.stats;
            }

            if (stats == null) return;


            Listing_Standard listing = new Listing_Standard();
            inrect.height = 9999.0f;
            listing.Begin(inrect);
            Text.Font = GameFont.Tiny;

            var sb = new StringBuilder();

            if (currentInformation.HasValue)
            {
                sb.AppendLine($" Method: {currentInformation.Value.methodName}, Mod: {currentInformation.Value.modName}");
                sb.AppendLine($" Assembly: {currentInformation.Value.assname}, Patches: {currentInformation.Value.patches.Count}");
                listing.Label(sb.ToString().TrimEndNewlines());
                listing.GapLine(2f);

                sb.Clear();
            }

            sb.AppendLine($" Total Entries: {stats.Entries}");
            sb.AppendLine($" Total Calls: {stats.TotalCalls}");
            sb.AppendLine($" Total Time: {stats.TotalTime:0.000}ms");

            sb.AppendLine($" Avg Time/Call: {stats.MeanTimePerCall:0.000}ms");
            sb.AppendLine($" Avg Calls/Update: {stats.MeanCallsPerUpdateCycle:0.00}");
            sb.AppendLine($" Avg Time/Update: {stats.MeanTimePerUpdateCycle:0.000}ms");

            sb.AppendLine($" Median Calls: {stats.MedianCalls}");
            sb.AppendLine($" Median Time: {stats.MedianTime}");
            sb.AppendLine($" Max Time: {stats.HighestTime:0.000}ms");
            sb.AppendLine($" Max Calls/Update: {stats.HighestCalls}");

            listing.Label(sb.ToString().TrimEndNewlines());

            DubGUI.ResetFont();

            listing.End();
        }
    }
}