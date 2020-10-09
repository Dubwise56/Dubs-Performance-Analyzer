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
        public static void DrawStats(Rect inrect)
        {
            var s = new LogStats();
            s.GenerateStats();

            lock (CurrentLogStats.sync)
            {
                var st = CurrentLogStats.stats;
                if (st != null)
                {
                    Text.Font = GameFont.Tiny;
                    Text.Anchor = TextAnchor.MiddleLeft;

                    var sb = new StringBuilder();
                    sb.AppendLine($" Total Entries: {st.Entries}");
                    sb.AppendLine($" Total Calls: {st.TotalCalls}");
                    sb.AppendLine($" Total Time: {st.TotalTime:0.000}ms");
                    sb.AppendLine($" Avg Time/Call: {st.MeanTimePerCall:0.000}ms");
                    sb.AppendLine($" Avg Calls/Update: {st.MeanCallsPerUpdateCycle:0.00}");
                    sb.AppendLine($" Avg Time/Update: {st.MeanTimePerUpdateCycle:0.000}ms");
                    sb.AppendLine($" Median Calls: {st.MedianCalls}");
                    sb.AppendLine($" Median Time: {st.MedianTime}");
                    sb.AppendLine($" Max Time: {st.HighestTime:0.000}ms");
                    sb.AppendLine($" Max Calls/Frame: {st.HighestCalls}");

                    Widgets.Label(inrect, sb.ToString().TrimEndNewlines());

                    DubGUI.ResetFont();
                }
            }
        }
    }
}