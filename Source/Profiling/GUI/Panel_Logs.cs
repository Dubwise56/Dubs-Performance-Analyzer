using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace Analyzer.Profiling
{
    public enum SortBy
    {
        Average,
        Max,
        AvPc,
        CallsPu,
        Percent,
        Name,
        Calls,
        Total,
        Spikes
    }

    public class Column : IExposable
    {
        public Column() {}
        public Column(SortBy i, bool a)
        {
            this.active = a;
            this.sortBy = i;
            this.total = 0;
            this.order = (int) i;
            if (i == SortBy.Name) order = 999;
        }

        public SortBy sortBy;
        public bool active;
        public double total;
        public int order;

        public string Name
        {
            get
            {
                switch (sortBy)
                {
                    case SortBy.Average: return Strings.logs_avpu(CurrentCycleName);
                    case SortBy.Max: return Strings.logs_maxpu(CurrentCycleName);
                    case SortBy.Calls: return Strings.logs_calls;
                    case SortBy.AvPc: return Strings.logs_avpc;
                    case SortBy.Percent: return Strings.logs_percent;
                    case SortBy.Name: return Strings.logs_name;
                    case SortBy.Total: return Strings.logs_total;
                    case SortBy.CallsPu: return Strings.logs_callspu(CurrentCycleName);
                    case SortBy.Spikes: return Strings.logs_spikes;
                }

                return null;
            }
        }

        private static string CurrentCycleName
            => GUIController.CurrentCategory == Category.Tick ? Strings.tab_tick : Strings.Frame;

        public string Desc
        {
            get
            {
                switch (sortBy)
                {
                    case SortBy.Average: return Strings.logs_av_desc;
                    case SortBy.Max: return Strings.logs_max_desc;
                    case SortBy.Calls: return Strings.logs_calls_desc;
                    case SortBy.AvPc: return Strings.logs_avpc_desc;
                    case SortBy.Percent: return Strings.logs_percent_desc;
                    case SortBy.Name: return Strings.logs_name_desc;
                    case SortBy.Total: return Strings.logs_total_desc;
                    case SortBy.CallsPu: return Strings.logs_callspu_desc(CurrentCycleName);
                    case SortBy.Spikes: return Strings.logs_spikes_desc;
                }

                return null;
            }
        }

        public string Value(ProfileLog log)
        {
            switch (sortBy)
            {
                case SortBy.Average: return $" {log.average:0.000}ms ";
                case SortBy.Max: return $" {log.max:0.000}ms ";
                case SortBy.Calls: return $" {log.calls.ToString("N0", CultureInfo.InvariantCulture)} ";
                case SortBy.AvPc:
                    var averagePerCall = 1000f * log.total / log.calls;
                    return $" {(averagePerCall < 10 ? $"{averagePerCall:0.00}"
                        : averagePerCall < 100 ? $"{averagePerCall:0.0}"
                        : $"{averagePerCall:0}")}us ";
                case SortBy.Percent:
                    return $" {log.percent * 100:0.00}% ";
                case SortBy.Name:
                    return "    " + (log.label ?? ProfileController.Profiles[log.key].label);
                case SortBy.Total:
                    return $" {log.total:0.000}ms ";
                case SortBy.CallsPu:
                    var num = log.calls / log.entries;
                    return num < 1 ? $" {num:F3}"
                        : num < 10 ? $" {num:F1}"
                        : $" {Convert.ToInt32(num)}";
                case SortBy.Spikes:
                    return log.spikes > 0 ? $" {log.spikes} " : "";
                default:
                    return "";
            }
        }

        public bool Active(Type curEntry) => active;

        public void ExposeData()
        {
            Scribe_Values.Look(ref sortBy, "sortBy");
            Scribe_Values.Look(ref active, "active");
            Scribe_Values.Look(ref order, "order");
        }
    }

    public static class Panel_Logs
    {
        private static Listing_Standard listing = new Listing_Standard { maxOneColumn = true };
        private static Rect viewFrustum;

        private const float BOX_HEIGHT = 40f;
        private const float ARBITRARY_OFFSET = 4f;
        private const float ARBITRARY_CLOSED_OFFSET = 12f;
        private const string NUMERICAL_DUMMY_STRING = " xxxx.xxxxms ";
        private const SortBy DEFAULT_SORTBY = SortBy.Average;

        private static float NUMERIC_WIDTH => Text.CalcSize(NUMERICAL_DUMMY_STRING).x + ARBITRARY_OFFSET;

        private static Vector2 ScrollPosition = Vector2.zero;

        public static List<Column> columns = null;

        public static void Initialise()
        {
            if (columns != null)
            {
                foreach (SortBy val in Enum.GetValues(typeof(SortBy)))
                    if (columns.All(c => c.sortBy != val)) columns.Add(new Column(val, val == SortBy.Spikes));
                columns.First(c => c.sortBy == SortBy.Percent).order = -1;
                return;
            }

            var enumLength = typeof(SortBy).GetEnumValues().Length;
            columns = new List<Column>();

            for(var i = 0; i < enumLength; i++)
            {
                var val = (SortBy) i;

                columns.Add(new Column(val, i <= 5 || val == SortBy.Spikes));
            }

            columns.First(c => c.sortBy == SortBy.Percent).order = -1;
        }


        public static void DrawLogs(Rect rect)
        {
            Widgets.DrawMenuSection(rect);

            if (!GUIController.CurrentEntry?.isPatched ?? true)
            {
                DubGUI.Heading(rect, $"Loading{GenText.MarchingEllipsis(0f)}");
                return;
            }

            var top = rect.TopPartPixels(RootBarHeight).ContractedBy(6f, 4f);
#if V1_7
            if (FrameBreakdown.parts.Count > 0)
                DrawFlame(top);
            else
#endif
            DrawRootBar(top.TopPartPixels(BLOCK_HEIGHT));
            rect.AdjustVerticallyBy(RootBarHeight);

            var columnsR = rect.TopPartPixels(COLUMN_HEADER_HEIGHT);
            DrawColumns(columnsR);
            
            for (var i = 0; i < columns.Count; i++)
                columns[i].total = 0d;

            rect.AdjustVerticallyBy(columnsR.height + 4);
            rect.height -= 2f;
            rect.width -= 2;

            Rect innerRect = rect.AtZero();
            innerRect.height = listing.curY;
            if (innerRect.height > rect.height)
            {
                innerRect.width -= 17f;
            }

            viewFrustum = rect.AtZero(); // Our view frustum starts at 0,0 from the rect we are given
            viewFrustum.y += ScrollPosition.y; // adjust our view frustum vertically based on the scroll position

            { // Begin scope for Scroll View
                Widgets.BeginScrollView(rect, ref ScrollPosition, innerRect);
                GUI.BeginGroup(innerRect);
                listing.Begin(innerRect);

                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Tiny;


                float currentListHeight = BOX_HEIGHT;

                Text.Anchor = TextAnchor.MiddleLeft;

                lock (Analyzer.LogicLock)
                {
                    foreach (ProfileLog log in Analyzer.Logs)
                    {
                        DrawLog(log, ref currentListHeight);
                    }
                }

                listing.End();
                GUI.EndGroup();
                Widgets.EndScrollView();
            }


            DubGUI.ResetFont();
        }

        private static void DrawColumns(Rect rect)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            GUI.color = Color.grey;
            Widgets.DrawLineHorizontal(rect.x, rect.yMax, rect.width);
            GUI.color = Color.white;

            var rhs = rect.RightPartPixels(30);
            rhs.x += 5;
            rhs.width -= 10;
            rhs.y = rhs.center.y - 10;
            rhs.height = 20;

            foreach (var column in columns.Where(c => c.Active(GUIController.CurrentEntry.type)).OrderBy(c => c.order))
            {
                if(column.sortBy == SortBy.Name)
                    Text.Anchor = TextAnchor.MiddleLeft;

                DrawColumnHeader(ref rect, column);
            }

            TooltipHandler.TipRegion(rhs, "Change what columns are visible");
            if(Widgets.ButtonImage(rhs, Textures.Gear))
            {
                var opts = new List<FloatMenuOption>();
                foreach (var col in columns)
                {
                    opts.Add(new FloatMenuOption(col.Name, () => col.active = !col.active, col.active ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex, Color.gray));
                }
                Find.WindowStack.Add(new FloatMenu(opts));
            }

            DubGUI.ResetFont();
        }

        private static void DrawColumnHeader(ref Rect inRect, Column c)
        {
            var rect = c.sortBy == SortBy.Name ? inRect with { width = inRect.width - 30f } : inRect.LeftPartPixels(NUMERIC_WIDTH);
            var sorted = Analyzer.SortBy == c.sortBy;

            Widgets.DrawHighlightIfMouseover(rect);
            GUI.color = sorted ? Color.white : new Color(0.65f, 0.65f, 0.65f);
            Text.Anchor = c.sortBy == SortBy.Name ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;

            var label = rect.ContractedBy(4f, 0f);
            if (c.sortBy == SortBy.Name) label.xMin += 16f;
            Text.WordWrap = false;
            var name = c.Name.Truncate(label.width);
            Widgets.Label(label, name);
            Text.WordWrap = true;

            if (sorted)
            {
                var textWidth = Text.CalcSize(name).x;
                Widgets.DrawBoxSolid(new Rect(c.sortBy == SortBy.Name ? label.x : label.xMax - textWidth, rect.yMax - 4f, textWidth, 2f), Color.white);
            }

            GUI.color = Color.white;

            if (Widgets.ButtonInvisible(rect) && Event.current.button == 0)
                Analyzer.SortBy = sorted ? DEFAULT_SORTBY : c.sortBy;

            var total = c.total == 0 ? "" : "\n\n" + (c.sortBy == SortBy.Percent ? Strings.logs_header_percenttotal(Math.Max(Math.Min(c.total, 1d), 0d).ToString("P1"))
                : c.sortBy == SortBy.Max ? Strings.logs_header_maxtotal(c.total.ToString("0.000")) : Strings.logs_header_mstotal(c.total.ToString("0.000")));
            Tip(rect, c.Desc + total + "\n\n" + (sorted ? Strings.logs_header_sorted : Strings.logs_header_clicktosort), "header." + c.sortBy);

            if (c.sortBy != SortBy.Name)
                inRect.AdjustHorizonallyBy(NUMERIC_WIDTH);
        }


        private static bool Matched(ProfileLog log, string s)
        {
            if (s == string.Empty)
            {
                Panel_TopRow.MatchType = string.Empty;
                return true;
            }

            if (log.meth != null && log.meth.Name.ContainsCaseless(s))
            {
                Panel_TopRow.MatchType = "Assembly";
                return true;
            }

            if (log.type != null && log.type.Assembly.FullName.ContainsCaseless(s))
            {
                Panel_TopRow.MatchType = "Assembly";
                return true;
            }

            if (log.label.ContainsCaseless(s))
            {
                Panel_TopRow.MatchType = "Label";
                return true;
            }

            return false;
        }

        private static void DrawLog(ProfileLog log, ref float currentListHeight)
        {
            if (log.pinned is false && Matched(log, Panel_TopRow.TimesFilter) is false)  {
                return;
            }

            columns[(int)SortBy.Average].total += log.average;

            ref var maxColumnTotal = ref columns[(int)SortBy.Max].total;
            maxColumnTotal = Math.Max(maxColumnTotal, log.max);
            
            columns[(int)SortBy.Percent].total += ProfileController.updateAverage > 0 ? log.self / ProfileController.updateAverage : 0d;

            var visible = listing.GetRect(BOX_HEIGHT);

            if (!visible.Overlaps(viewFrustum)) // if we don't overlap, continue, but continue to adjust for further logs.
            {
                listing.GapLine(0f);
                currentListHeight += (BOX_HEIGHT + 4);

                return;
            }

            var profile = ProfileController.Profiles[log.key];

            // Is this entry currently 'active'?
            if (GUIController.CurrentEntry.onSelect != null)
            {
                OnSelect(log, profile, out var active);

                // Show a button to toggle whether an entry is 'active'
                if (GUIController.CurrentEntry.checkBox != null)
                    Checkbox(ref visible, log, profile, ref active);
            }

            Widgets.DrawHighlightIfMouseover(visible);

            if (GUIController.CurrentProfiler?.key == profile.key)
                Widgets.DrawHighlightSelected(visible);

            // onclick work, left click view stats, right click internal patch, ctrl + left click unpatch
            if (Widgets.ButtonInvisible(visible))
                ClickWork(log, profile);

            var root = ProfileController.updateAverage;
            if (root > 0)
            {
                var bar = visible.BottomPartPixels(6f);
                var colour = rowColours.TryGetValue(log.key, out var c) ? c : Color.grey;
                Widgets.DrawBoxSolid(bar with { width = bar.width * Mathf.Clamp01((float)(log.average / root)) }, colour with { a = 0.3f });
                Widgets.DrawBoxSolid(bar with { width = bar.width * Mathf.Clamp01((float)(log.self / root)) }, colour);
                Tip(bar, Strings.logs_rowbar_tip(log.self.ToString("0.000"), (log.self / root).ToString("P1"), log.average.ToString("0.000"), (log.average / root).ToString("P1"), log.entries.ToString()), "rowbar." + log.key);
            }

            Text.Anchor = TextAnchor.MiddleRight;

            foreach (var column in columns.Where(c => c.Active(GUIController.CurrentEntry.type)).OrderBy(c => c.order))
            {
                DrawColumnContents(ref visible, column, column.Value(log), profile, log);
            }

            GUI.color = Color.white;

            listing.GapLine(0f);
            currentListHeight += (BOX_HEIGHT + 4);
        }

        public static void DrawColumnContents(ref Rect rect, Column c, string value, Profiler profile, ProfileLog log = null)
        {
            if (c.sortBy == SortBy.Name)
            {
                if (profile.pinned)
                {
                    var iconRect = new Rect(rect.x, rect.y + rect.height/4.0f, Text.LineHeight, Text.LineHeight);
                    rect.x += Text.LineHeight;

                    GUI.DrawTexture(iconRect, Textures.pin);
                }

                if (log != null && GUIController.CurrentCategory == Category.Patches)
                    value += $" <color=#999999>({Phases(log.seenIn)})</color>";

                Text.Anchor = TextAnchor.MiddleLeft;
                rect.width = 10000;
            }

            if (c.sortBy == SortBy.Spikes && log is { spike: > 0 })
                Tip(rect.LeftPartPixels(NUMERIC_WIDTH), log.spikeText, "spike." + log.key);

            Widgets.Label(c.sortBy == SortBy.Name ? rect : rect.LeftPartPixels(NUMERIC_WIDTH), value);
            rect.x += NUMERIC_WIDTH;
        }

        private const float BLOCK_HEIGHT = 34f;
        private const float COLUMN_HEADER_HEIGHT = 26f;

#if V1_7
        private static float RootBarHeight => 8f + (FrameBreakdown.parts.Count > 0 ? 18f + 13f * (ProfileController.WholeFrameRoot ? 2 : 3) + 4f + 18f : BLOCK_HEIGHT);
#else
        private static float RootBarHeight => 8f + BLOCK_HEIGHT;
#endif

        private static Rect Heading(Rect block) => new(block.x, block.y, block.width, 18f);

        private static Rect Bar(Rect block) => new(block.x, block.y + 19f, block.width, 10f);

        private static void HeadingLabel(Rect rect, string text, string tip, string id)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(rect, text.Truncate(rect.width));
            Text.WordWrap = true;
            Tip(rect, tip, id);
        }

        private static void Tip(Rect rect, string text, string id) => TooltipHandler.TipRegion(rect, new TipSignal(text, ("PA.Logs." + id).GetHashCode()));

        private static readonly Dictionary<string, Color> rowColours = new();


        private static readonly Color[] BarColours = [new(0.36f, 0.62f, 0.9f), new(0.95f, 0.6f, 0.25f), new(0.45f, 0.8f, 0.45f), new(0.85f, 0.4f, 0.75f), new(0.9f, 0.85f, 0.35f), new(0.4f, 0.8f, 0.85f)];

        private static string Phases(int seen)
        {
            var parts = new List<string>();
            if ((seen & ProfileController.PhaseTick) != 0) parts.Add(Strings.phase_tick);
            if ((seen & ProfileController.PhaseUpdate) != 0) parts.Add(Strings.phase_update);
            if ((seen & ProfileController.PhaseGUI) != 0) parts.Add(Strings.phase_interface);
            if ((seen & ProfileController.PhaseThread) != 0) parts.Add(Strings.phase_threads);
            return parts.Count == 0 ? Strings.phase_none : string.Join(", ", parts);
        }

        private static (string title, string noun, string unit, string units) RootNames()
        {
            if (ProfileController.WholeFrameRoot) return (Strings.root_frame_title, Strings.root_frame_noun, Strings.unit_frame, Strings.unit_frames);
            return GUIController.CurrentCategory == Category.Tick
                ? (Strings.root_tick_title, Strings.root_tick_noun, Strings.unit_tick, Strings.unit_ticks)
                : (Strings.root_update_title, Strings.root_update_noun, Strings.unit_frame, Strings.unit_frames);
        }

        private static void DrawRootBar(Rect rect)
        {
            List<ProfileLog> logs;
            lock (Analyzer.LogicLock) logs = Analyzer.Logs.Where(l => l.self > 0).OrderByDescending(l => l.self).ToList();

            var root = ProfileController.updateAverage;
            var listed = logs.Sum(l => l.self);
            var (title, noun, unit, units) = RootNames();

            var samples = Analyzer.GetCurrentLogCount;

            rowColours.Clear();
            for (var i = 0; i < logs.Count; i++)
                rowColours[logs[i].key] = i < BarColours.Length ? BarColours[i] : Color.grey;

            HeadingLabel(Heading(rect), Strings.root_heading(title, root.ToString("0.000"), unit, samples.ToString("N0"), units, listed.ToString("0.000"), (root > 0 ? listed / root : 0).ToString("P1")),
                Strings.root_heading_tip(samples.ToString("N0"), units, noun, listed.ToString("0.000")), "root.heading");

            var bar = Bar(rect);
            Widgets.DrawBoxSolid(bar, new Color(0.15f, 0.15f, 0.15f));

            var x = bar.x;
            for (var i = 0; i <= BarColours.Length && i < logs.Count && root > 0; i++)
            {
                var time = i < BarColours.Length ? logs[i].self : logs.Skip(i).Sum(l => l.self);
                var segment = new Rect(x, bar.y, Mathf.Min(bar.width * (float)(time / root), bar.xMax - x), bar.height);
                Widgets.DrawBoxSolid(segment, i < BarColours.Length ? BarColours[i] : Color.grey);
                Tip(segment, Strings.root_segment_tip(i < BarColours.Length ? logs[i].label : Strings.root_otherrows((logs.Count - i).ToString()), time.ToString("0.000"), unit, (time / root).ToString("P1"), noun), "root.segment." + i);
                x = segment.xMax;
            }

            Tip(bar with { xMin = x }, Strings.root_rest_tip(noun), "root.rest");

            Text.Anchor = TextAnchor.UpperLeft;
        }

#if V1_7
        private static readonly Color[] FrameColours = [new(0.35f, 0.75f, 0.7f), new(0.45f, 0.5f, 0.9f), new(0.7f, 0.45f, 0.8f), new(0.5f, 0.5f, 0.5f), new(0.24f, 0.3f, 0.24f)];

        private static string[] FrameTips => [Strings.frame_tip_poll, Strings.frame_tip_update, Strings.frame_tip_capture, Strings.frame_tip_other, Strings.frame_tip_waiting];

        private static void FlameLabel(Rect rect, string text)
        {
            Text.Anchor = TextAnchor.MiddleRight;
            GUI.color = new Color(0.65f, 0.65f, 0.65f);
            Widgets.Label(rect with { y = rect.y - 4f, height = rect.height + 8f }, text);
            GUI.color = Color.white;
        }

        private static void DrawFlame(Rect rect)
        {
            List<ProfileLog> logs;
            lock (Analyzer.LogicLock) logs = Analyzer.Logs.Where(l => l.self > 0).OrderByDescending(l => l.self).ToList();

            rowColours.Clear();
            for (var i = 0; i < logs.Count; i++)
                rowColours[logs[i].key] = i < BarColours.Length ? BarColours[i] : Color.grey;

            var category = GUIController.CurrentCategory;
            var parent = ProfileController.WholeFrameRoot ? (category == Category.GUI ? 2 : -1) : 1;
            var nested = !ProfileController.WholeFrameRoot;
            var perFrame = category == Category.Tick ? (GUIElement_TPS.FPS > 0 ? GUIElement_TPS.TPS / (double)GUIElement_TPS.FPS : 0d) : 1d;
            var (title, noun, unit, units) = RootNames();

            var frame = FrameBreakdown.frameMs;
            var root = ProfileController.updateAverage;
            var listed = logs.Sum(l => l.self);
            var samples = Analyzer.GetCurrentLogCount;
            var ticks = category == Category.Tick ? " " + Strings.frame_ticksperframe(perFrame.ToString("0.#")) : "";

            HeadingLabel(Heading(rect), Strings.frame_heading(frame.ToString("0.000"), (frame > 0 ? 1000d / frame : 0).ToString("0"), FrameBreakdown.samples.ToString()), Strings.frame_heading_tip, "frame.heading");

            const float gutter = 70f;
            var bar = new Rect(rect.x + gutter, rect.y + 19f, rect.width - gutter, 10f);
            var scale = frame > 0 ? bar.width / frame : 0d;
            FlameLabel(new Rect(rect.x, bar.y, gutter - 6f, bar.height), Strings.frame_label_whole);
            FlameLabel(new Rect(rect.x, bar.y + 13f, gutter - 6f, bar.height), nested ? (category == Category.Tick ? Strings.frame_label_ticks : Strings.frame_label_update) : Strings.frame_label_tab);
            if (nested) FlameLabel(new Rect(rect.x, bar.y + 26f, gutter - 6f, bar.height), Strings.frame_label_tab);
            Widgets.DrawBoxSolid(bar, new Color(0.15f, 0.15f, 0.15f));

            var x = bar.x;
            var anchor = bar.x;
            for (var i = 0; i < FrameBreakdown.parts.Count && frame > 0; i++)
            {
                var (label, ms) = FrameBreakdown.parts[i];
                var segment = new Rect(x, bar.y, Mathf.Min((float)(ms * scale), bar.xMax - x), bar.height);
                Widgets.DrawBoxSolid(segment, FrameColours[i]);
                if (i == parent) anchor = segment.x;
                if (i == parent || parent < 0 && i < 4) Widgets.DrawBox(segment);
                if (i == 4 && segment.width > 140f)
                {
                    Text.Anchor = TextAnchor.MiddleCenter;
                    GUI.color = new Color(0.75f, 0.85f, 0.75f);
                    Widgets.Label(segment with { y = segment.y - 4f, height = segment.height + 8f }, Strings.frame_waiting);
                    GUI.color = Color.white;
                }
                Tip(segment, Strings.frame_segment_tip(FrameTips[i], ms.ToString("0.000"), (ms / frame).ToString("P1")), "frame.segment." + i);
                x = segment.xMax;
            }

            var y = bar.yMax + 3f;
            if (nested)
            {
                var rootFrame = root * perFrame;
                var rootRect = new Rect(anchor, y, Mathf.Max(1f, (float)(rootFrame * scale)), 10f);
                Widgets.DrawBoxSolid(rootRect, FrameColours[1] * 0.7f + Color.white * 0.3f);
                Widgets.DrawBox(rootRect);
                Tip(rootRect, Strings.frame_root_tip(title, root.ToString("0.000"), unit, ticks, rootFrame.ToString("0.000"), (frame > 0 ? rootFrame / frame : 0).ToString("P1")), "flame.root");
                y += 13f;
            }

            x = anchor;
            for (var i = 0; i <= BarColours.Length && i < logs.Count && root > 0; i++)
            {
                var time = i < BarColours.Length ? logs[i].self : logs.Skip(i).Sum(l => l.self);
                var segment = new Rect(x, y, (float)(time * perFrame * scale), 10f);
                Widgets.DrawBoxSolid(segment, i < BarColours.Length ? BarColours[i] : Color.grey);
                Tip(segment, Strings.frame_row_tip(i < BarColours.Length ? logs[i].label : Strings.root_otherrows((logs.Count - i).ToString()), time.ToString("0.000"), unit, ticks, (time * perFrame).ToString("0.000"), (time / root).ToString("P1"), noun, (frame > 0 ? time * perFrame / frame : 0).ToString("P2")), "flame.row." + i);
                x = segment.xMax;
            }

            HeadingLabel(new Rect(rect.x, y + 14f, rect.width, 18f), Strings.frame_tab_heading(title, root.ToString("0.000"), unit, samples.ToString("N0"), units, listed.ToString("0.000"), (root > 0 ? listed / root : 0).ToString("P1")),
                Strings.frame_tab_heading_tip(samples.ToString("N0"), units, noun, listed.ToString("0.000")), "root.heading");

            Text.Anchor = TextAnchor.UpperLeft;
        }
#endif

        public static void ClickWork(ProfileLog log, Profiler profile)
        {
            if (Event.current.button == 0) // left click
            {
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    GUIController.CurrentEntry.onClick?.Invoke(null, new object[] { profile, log });
                    Modbase.Settings.Write();
                }
                else {
                    var old = GUIController.CurrentProfiler;
                    
                    GUIController.CurrentProfiler = GUIController.CurrentProfiler == profile ? null : profile;
                    Panel_BottomRow.NotifyNewProfiler(old, GUIController.CurrentProfiler);
                }
            }
            else if (Event.current.button == 1) // right click
            {
                if (log.meth == null) return;

                var options = RightClickDropDown(log, profile).ToList();

                if (options.Count != 0) Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        public static void OnSelect(ProfileLog log, Profiler profile, out bool active)
        {
            active = (bool)GUIController.CurrentEntry.onSelect.Invoke(null, new object[] { profile, log });
        }

        public static void Checkbox(ref Rect rect, ProfileLog log, Profiler profile, ref bool active)
        {
            var checkboxRect = new Rect(rect.x, rect.y, 25f, rect.height);
            rect.x += 25f;
            if (DubGUI.Checkbox(checkboxRect, "", ref active))
            {
                GUIController.CurrentEntry.checkBox.Invoke(null, new object[] { log });
                Modbase.Settings.Write();
            }
        }


        private static IEnumerable<FloatMenuOption> RightClickDropDown(ProfileLog log, Profiler profiler)
        {
            var meth = log.meth as MethodInfo;

            if (Input.GetKey(KeyCode.LeftShift))
            {

                if (GUIController.CurrentEntry.name.Contains("Harmony")) // we can return an 'unpatch' for methods in a harmony tab
                    yield return new FloatMenuOption("Unpatch Method (Destructive)", () => Utility.UnpatchMethod(meth));

                yield return new FloatMenuOption("Unpatch methods that patch (Destructive)", () => Utility.UnpatchMethodsOnMethod(meth));
            }

            var message = profiler.pinned ? "Unpin profile from entry" : "Pin profile to the top of the entry";
            yield return new FloatMenuOption(message, () => profiler.pinned = !profiler.pinned);

            if (GUIController.CurrentEntry.type != typeof(H_HarmonyTranspilersInternalMethods))
                yield return new FloatMenuOption("Profile the internal methods of", () => Utility.PatchInternalMethod(meth, GUIController.CurrentCategory));



            if (meth != null) {
                yield return new FloatMenuOption("Copy", () => {
                    var te = new TextEditor { text = Utility.GetSignature(meth, false) };
                    te.SelectAll();
                    te.Copy();
                });
                
                yield return new FloatMenuOption("Save to Custom Tick", () => {
                    Settings.SavedPatches_Tick.Add(string.Concat(meth.DeclaringType, ":", meth.Name));
                    Modbase.Settings.Write();    
                });
                yield return new FloatMenuOption("Save to Custom Update", () => {
                    Settings.SavedPatches_Update.Add(string.Concat(meth.DeclaringType, ":", meth.Name));
                    Modbase.Settings.Write();    
                });
            }
            
            // This part is WIP - it would require the ability to change the tab a method is active in on the fly
            // which is possible (with a transpiler to the current transpiler) but it would likely end up being
            // quite ugly, and I'd rather give a little more thought to the problem
            //yield return new FloatMenuOption("Profile in Custom Tab", () =>
            //{
            //    if (GUIController.CurrentCategory == Category.Tick)
            //    {
            //        MethodTransplanting.UpdateMethod(typeof(CustomProfilersTick), meth);
            //        GUIController.SwapToEntry("Custom Tick");
            //    }
            //    else
            //    {
            //        MethodTransplanting.UpdateMethod(typeof(CustomProfilersUpdate), meth);
            //        GUIController.SwapToEntry("Custom Update");
            //    }
            //});
        }
    }
}
