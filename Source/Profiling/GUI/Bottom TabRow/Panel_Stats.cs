using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    public static class Panel_Stats
    {
        private static Vector2 scrolls = Vector2.zero;
        private static readonly Listing_Standard listing = new Listing_Standard { maxOneColumn = true };

        public static void DrawStats(Rect inrect, GeneralInformation? currentInformation)
        {
            var stats = new LogStats();
            stats.GenerateStats();

            stats = null;

            lock (CurrentLogStats.sync)
            {
                stats = CurrentLogStats.stats;
            }

            if (stats == null) return;

            inrect = inrect.ContractedBy(4f);
            var r = inrect;
            r.height = listing.CurHeight;
            r.width = 18;
            Widgets.BeginScrollView(inrect, ref scrolls, r);

            listing.Begin(inrect);
            Text.Font = GameFont.Tiny;

            var sb = new StringBuilder();

            if (currentInformation.HasValue)
            {
                sb.AppendLine(
                    $"Method: {currentInformation.Value.methodName}, Mod: {currentInformation.Value.modName}");
                sb.AppendLine(
                    $"Assembly: {currentInformation.Value.assname}, Patches: {currentInformation.Value.patches.Count}");

                var modLabel = sb.ToString().TrimEndNewlines();
                var rect = listing.GetRect(Text.CalcHeight(modLabel, listing.ColumnWidth));

                Widgets.Label(rect, modLabel);
                Widgets.DrawHighlightIfMouseover(rect);

                if (Input.GetMouseButtonDown(1) && rect.Contains(Event.current.mousePosition)) // mouse button right
                {
                    var options = new List<FloatMenuOption>
                    {
                        new FloatMenuOption("Open In Github",
                            () => Panel_BottomRow.OpenGithub(
                                $"{currentInformation.Value.typeName}.{currentInformation.Value.methodName}")),
                        new FloatMenuOption("Open In Dnspy (requires local path)",
                            () => Panel_BottomRow.OpenDnspy(currentInformation.Value.method))
                    };

                    Find.WindowStack.Add(new FloatMenu(options));
                }

                listing.GapLine(2f);

                sb.Clear();
            }

            sb.AppendLine($"Total Entries:".Colorize(Color.grey) + $" { stats.Entries}");
            sb.AppendLine($"Total Calls:".Colorize(Color.grey) + $" {stats.TotalCalls}");
            sb.AppendLine($"Total Time:".Colorize(Color.grey) + $" {stats.TotalTime:0.000}ms");

            sb.AppendLine($"Avg Time/Call:".Colorize(Color.grey) + $" {stats.MeanTimePerCall:0.000}ms");
            sb.AppendLine($"Avg Calls/Update:".Colorize(Color.grey) + $" {stats.MeanCallsPerUpdateCycle:0.00}");
            sb.AppendLine($"Avg Time/Update:".Colorize(Color.grey) + $" {stats.MeanTimePerUpdateCycle:0.000}ms");

            sb.AppendLine($"Median Calls:".Colorize(Color.grey) + $" {stats.MedianCalls}");
            sb.AppendLine($"Median Time:".Colorize(Color.grey) + $" {stats.MedianTime}");
            sb.AppendLine($"Max Time:".Colorize(Color.grey) + $" {stats.HighestTime:0.000}ms");
            sb.AppendLine($"Max Calls/Update:".Colorize(Color.grey) + $" {stats.HighestCalls}");

            listing.Label(sb.ToTaggedString().Trim());

            DubGUI.ResetFont();

            listing.End();

            Widgets.EndScrollView();
        }
    }
}