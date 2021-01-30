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
        Max, Average, Percent, Total, Calls, Name
    }

    public struct TOTALS
    {
        public double Max, Average, Percent, Total, Calls, Name;
    }

    public static class Panel_Logs
    {
        private static Listing_Standard listing = new Listing_Standard { maxOneColumn = true };
        private static Rect viewFrustum;



        private const float BOX_HEIGHT = 40f;
        private const float LABEL_OFFSET = 200f;
        private const float ARBITRARY_OFFSET = 4f;
        private const float ARBITRARY_CLOSED_OFFSET = 12f;
        private const string NUMERICAL_DUMMY_STRING = " xxxx.xxxxms ";
        private const SortBy DEFAULT_SORTBY = SortBy.Percent;

        private static float NUMERIC_WIDTH => Text.CalcSize(NUMERICAL_DUMMY_STRING).x + ARBITRARY_OFFSET;


        private static Vector2 ScrollPosition = Vector2.zero;

        public static string tipCache = "";
        public static string tipLabelCache = "";

        public static bool[] columns = { true, true, true, true, true, true };

        public static TOTALS totals;


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

            totals = new TOTALS();

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
            // [ Max ] [ Average ] [ Percent ] [ Total ] [ Calls ] [ Name ] 

            DrawColumnHeader(ref rect, Strings.logs_max, Strings.logs_max_desc, SortBy.Max, NUMERIC_WIDTH, $"{totals.Max:0.000}ms");
            DrawColumnHeader(ref rect, Strings.logs_av, Strings.logs_av_desc, SortBy.Average, NUMERIC_WIDTH, $"{totals.Average:0.000}ms");

            DrawColumnHeader(ref rect, Strings.logs_percent, Strings.logs_percent_desc, SortBy.Percent, NUMERIC_WIDTH, $"{totals.Percent * 100:0.0}%");
            DrawColumnHeader(ref rect, Strings.logs_total, Strings.logs_total_desc, SortBy.Total, NUMERIC_WIDTH, $"{totals.Total:0.000}ms");

            if (GUIController.CurrentEntry.type != typeof(H_HarmonyTranspilersInternalMethods))
                DrawColumnHeader(ref rect, Strings.logs_calls, Strings.logs_calls_desc, SortBy.Calls, NUMERIC_WIDTH, $"{totals.Calls.ToString("N0", CultureInfo.InvariantCulture)}");
            // give the name 'infinite' width so there is no wrapping
            // Set text anchor to middle left so we can see our text
            // offset by four chars to make it look offset
            Text.Anchor = TextAnchor.MiddleLeft;
            DrawColumnHeader(ref rect, "    " + Strings.logs_name, Strings.logs_name_desc, SortBy.Name, 10000);
            DubGUI.ResetFont();
        }

        private static void DrawColumnHeader(ref Rect inRect, string name, string desc, SortBy value, float width, string totalReadout = "")
        {
            bool closed = false;
            if (!columns[(int)value]) // If our column is currently collapsed
            {
                if (value != SortBy.Name)
                    width = ARBITRARY_CLOSED_OFFSET;
                closed = true;
            }

            Widgets.DrawOptionBackground(inRect, false);

            var rect = inRect.LeftPartPixels(width);

            if (closed is false)
            {
                //  Text.Font = GameFont.Small;
                Widgets.Label(rect.TopHalf(), name);
                if (totalReadout != string.Empty)
                {
                    // Text.Font = GameFont.Tiny;
                    Widgets.Label(rect.BottomHalf(), totalReadout);
                }
            }

            if (Analyzer.SortBy == value) Widgets.DrawHighlight(rect);

            if (Widgets.ButtonInvisible(rect))
            { // sort by 'max'
                if (Event.current.button == 0) // left click, change sort by
                {
                    if (Analyzer.SortBy == value) Analyzer.SortBy = DEFAULT_SORTBY;
                    else Analyzer.SortBy = value;
                }
                else // middle / right, close the tab
                {
                    columns[(int)value] = !columns[(int)value];
                }
            }
            TooltipHandler.TipRegion(rect, desc);

            if (value != SortBy.Name)
            {
                inRect.AdjustHorizonallyBy(width);

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

            if (log.def != null && log.def.defName.ContainsCaseless(s))
            {
                Panel_TopRow.MatchType = "Def";
                return true;
            }

            if (log.mod.ContainsCaseless(s))
            {
                Panel_TopRow.MatchType = "Mod";
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
            
            if (Matched(log, Panel_TopRow.TimesFilter) is false)
            {
                return;
            }

            totals.Max += log.max;
            totals.Average += log.average;
            totals.Percent += log.percent;
            totals.Total += log.total;
            totals.Calls += log.calls;

            Rect visible = listing.GetRect(BOX_HEIGHT);

            if (!visible.Overlaps(viewFrustum)) // if we don't overlap, continue, but continue to adjust for further logs.
            {
                listing.GapLine(0f);
                currentListHeight += (BOX_HEIGHT + 4);

                return;
            }

            Profiler profile = ProfileController.Profiles[log.key];


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

            // onhover tooltip //doesn't actually work properly so fuck that off
          //  if (Mouse.IsOver(visible))
             //   DrawHover(log, visible);

            // onclick work, left click view stats, right click internal patch, ctrl + left click unpatch
            if (Widgets.ButtonInvisible(visible))
                ClickWork(log, profile);

            // Colour a fillable bar below the log depending on the % fill of a log
            var colour = Textures.grey;
            //  if (log.percent <= .25f) colour = Textures.grey; // <= 25%
            //  else if (log.percent <= .75f) colour = Textures.blue; //  25% < x <=75%
            //   else if (log.percent <= .999) colour = Textures.red; // 75% < x <= 99.99% (we want 100% to be grey)

            Widgets.FillableBar(visible.BottomPartPixels(8f), log.percent, colour, Textures.clear, false);

            Text.Anchor = TextAnchor.MiddleCenter;

            DrawColumnContents(ref visible, $" {log.max:0.000}ms ", SortBy.Max);
            DrawColumnContents(ref visible, $" {log.average:0.000}ms ", SortBy.Average);
            DrawColumnContents(ref visible, $" {log.percent * 100:0.0}% ", SortBy.Percent);
            DrawColumnContents(ref visible, $" {log.total:0.000}ms ", SortBy.Total);

            if (GUIController.CurrentEntry.type != typeof(H_HarmonyTranspilersInternalMethods))
                DrawColumnContents(ref visible, $" {log.calls.ToString("N0", CultureInfo.InvariantCulture)} ", SortBy.Calls);


            Text.Anchor = TextAnchor.MiddleLeft;
            visible.width = 10000;
            DrawColumnContents(ref visible, "    " + log.label, SortBy.Name);

            GUI.color = Color.white;

            listing.GapLine(0f);
            currentListHeight += (BOX_HEIGHT + 4);
        }

        public static void DrawColumnContents(ref Rect rect, string str, SortBy value)
        {

            if (columns[(int)value])
            {
                Widgets.Label(value == SortBy.Name ? rect : rect.LeftPartPixels(NUMERIC_WIDTH), str);
                rect.x += NUMERIC_WIDTH;
            }
            else
            {
                rect.x += ARBITRARY_CLOSED_OFFSET;
            }
        }

        // doesn't work properly
        //public static void DrawHover(ProfileLog log, Rect visible)
        //{
        //    if (log.meth != null)
        //    {
        //        if (log.label != tipLabelCache) // If we have a new label, re-create the string, else use our cached version.
        //        {
        //            tipLabelCache = log.label;
        //            StringBuilder builder = new StringBuilder();
        //            Patches patches = Harmony.GetPatchInfo(log.meth);
        //            if (patches != null)
        //            {
        //                foreach (Patch patch in patches.Prefixes) GetString("Prefix", patch);
        //                foreach (Patch patch in patches.Postfixes) GetString("Postfix", patch);
        //                foreach (Patch patch in patches.Transpilers) GetString("Transpiler", patch);
        //                foreach (Patch patch in patches.Finalizers) GetString("Finalizer", patch);

        //                void GetString(string type, Patch patch)
        //                {
        //                    if (Utility.IsNotAnalyzerPatch(patch.owner))
        //                    {
        //                        if (patch.PatchMethod.DeclaringType != null)
        //                        {
        //                            string ass = patch.PatchMethod.DeclaringType.Assembly.FullName;
        //                            string modName = "Unknown";
        //                            try
        //                            {
        //                                modName = ModInfoCache.AssemblyToModname[ass];
        //                            }
        //                            catch { }

        //                            builder.AppendLine($"{type} from {modName} with the index {patch.index} and the priority {patch.priority}\n");
        //                        }
        //                    }
        //                }

        //                tipCache = builder.ToString();
        //            }
        //        }
        //        TooltipHandler.TipRegion(visible, tipCache);
        //    }
        //}

        public static void ClickWork(ProfileLog log, Profiler profile)
        {
            if (Event.current.button == 0) // left click
            {
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    GUIController.CurrentEntry.onClick?.Invoke(null, new object[] { profile, log });
                    Modbase.Settings.Write();
                }
                else
                {
                    if (GUIController.CurrentProfiler == profile)
                        GUIController.CurrentProfiler = null;
                    else // This is now the 'active' profile  
                        GUIController.CurrentProfiler = profile;
                }
            }
            else if (Event.current.button == 1) // right click
            {
                if (log.meth == null) return;

                var options = RightClickDropDown(log).ToList();

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


        private static IEnumerable<FloatMenuOption> RightClickDropDown(ProfileLog log)
        {
            var meth = log.meth as MethodInfo;

            if (Input.GetKey(KeyCode.LeftShift))
            {

                if (GUIController.CurrentEntry.name.Contains("Harmony")) // we can return an 'unpatch' for methods in a harmony tab
                    yield return new FloatMenuOption("Unpatch Method (Destructive)", () => Utility.UnpatchMethod(meth));

                yield return new FloatMenuOption("Unpatch methods that patch (Destructive)", () => Utility.UnpatchMethodsOnMethod(meth));
            }

            if (GUIController.CurrentEntry.type != typeof(H_HarmonyTranspilersInternalMethods))
                yield return new FloatMenuOption("Profile the internal methods of", () => Utility.PatchInternalMethod(meth, GUIController.CurrentCategory));

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
