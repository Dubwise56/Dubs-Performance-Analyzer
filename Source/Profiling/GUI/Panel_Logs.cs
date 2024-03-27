using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace Analyzer.Profiling
{
    public enum SortBy
    {
        Average,
        Max, 
        Calls, 
        AvPc,
        Percent,
        Name,
        Total,
        CallsPu
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
                    case SortBy.Average: return Strings.logs_av;
                    case SortBy.Max: return Strings.logs_max;
                    case SortBy.Calls: return Strings.logs_calls;
                    case SortBy.AvPc: return Strings.logs_avpc;
                    case SortBy.Percent: return Strings.logs_percent;
                    case SortBy.Name: return Strings.logs_name;
                    case SortBy.Total: return Strings.logs_total;
                    case SortBy.CallsPu: return Strings.logs_callspu(GUIController.CurrentCategory == Category.Tick ? "Tick" : "Update");
                }

                return null;
            }
        }

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
                    case SortBy.CallsPu: return Strings.logs_callspu_desc(GUIController.CurrentCategory == Category.Tick ? "Tick" : "Update");
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
                case SortBy.AvPc: return $" {log.total/log.calls:0.000}ms ";
                case SortBy.Percent: return $" {log.percent * 100:0.0}% ";
                case SortBy.Name: return "    " + log.label;
                case SortBy.Total: return $" {log.total:0.000}ms ";
                case SortBy.CallsPu:
                    var num = log.calls / log.entries;
                    return num < 1 ? $" {num:F3}" : $" {(int)Math.Round(num)}";
            }

            return "";
        }

        public bool Active(Type curEntry)
        {
            var transpilersType = typeof(H_HarmonyTranspilersInternalMethods);

            if ((sortBy == SortBy.Calls || sortBy == SortBy.AvPc || sortBy == SortBy.CallsPu) && transpilersType == curEntry)
                return false;

            return active;
        }

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
            if (columns != null) return;

            var enumLength = typeof(SortBy).GetEnumValues().Length;
            columns = new List<Column>();

            for(var i = 0; i < enumLength; i++)
            {
                var val = (SortBy) i;

                columns.Add(new Column(val, i <= 5));
            }
        }


        public static void DrawLogs(Rect rect)
        {
            Widgets.DrawMenuSection(rect);

            if (!GUIController.CurrentEntry?.isPatched ?? true)
            {
                DubGUI.Heading(rect, $"Loading{GenText.MarchingEllipsis(0f)}");
                return;
            }

            var columnsR = rect.TopPartPixels(50f);
            DrawColumns(columnsR);

            columns[(int)SortBy.Average].total = 0;

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
            Widgets.DrawOptionBackground(inRect, false);

            var rect = inRect.LeftPartPixels(NUMERIC_WIDTH);

            if (c.total != 0)
            {
                Widgets.Label(rect.TopHalf(), c.Name);
                Widgets.Label(rect.BottomHalf(), $"{c.total:0.000}ms");
            }
            else
            {
                Widgets.Label(rect, (c.sortBy == SortBy.Name ? "    " : "") + c.Name);
            }
            
            if (Analyzer.SortBy == c.sortBy) 
                Widgets.DrawHighlight(rect);

            if (Widgets.ButtonInvisible(rect))
            { 
                if (Event.current.button == 0) // left click, change sort by
                {
                    Analyzer.SortBy = Analyzer.SortBy == c.sortBy ? DEFAULT_SORTBY : c.sortBy;
                }
            }

            TooltipHandler.TipRegion(rect, c.Desc);

            if (c.sortBy != SortBy.Name)
            {
                inRect.AdjustHorizonallyBy(NUMERIC_WIDTH);

                GUI.color = Color.grey;
                Widgets.DrawLineVertical(inRect.x, rect.y, rect.height);
                GUI.color = Color.white;
            }
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

            // Colour a fillable bar below the log depending on the % fill of a log
            var colour = Textures.grey;
            if (log.percent <= .25f) colour = Textures.grey; // <= 25%
            else if (log.percent <= .75f) colour = Textures.blue; //  25% < x <=75%
            else if (log.percent <= .999) colour = Textures.red; // 75% < x <= 99.99% (we want 100% to be grey)

            Widgets.FillableBar(visible.BottomPartPixels(8f), log.percent, colour, Textures.clear, false);

            Text.Anchor = TextAnchor.MiddleCenter;

            foreach (var column in columns.Where(c => c.Active(GUIController.CurrentEntry.type)).OrderBy(c => c.order))
            {
                DrawColumnContents(ref visible, column, column.Value(log), profile);
            }

            GUI.color = Color.white;

            listing.GapLine(0f);
            currentListHeight += (BOX_HEIGHT + 4);
        }

        public static void DrawColumnContents(ref Rect rect, Column c, string value, Profiler profile)
        {
            if (c.sortBy == SortBy.Name)
            {
                if (profile.pinned)
                {
                    var iconRect = new Rect(rect.x, rect.y + rect.height/4.0f, Text.LineHeight, Text.LineHeight);
                    rect.x += Text.LineHeight;

                    GUI.DrawTexture(iconRect, Textures.pin);
                }

                Text.Anchor = TextAnchor.MiddleLeft;
                rect.width = 10000;
            }

            Widgets.Label(c.sortBy == SortBy.Name ? rect : rect.LeftPartPixels(NUMERIC_WIDTH), value);
            rect.x += NUMERIC_WIDTH;
        }

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
