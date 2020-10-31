using ColourPicker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;

namespace Analyzer.Profiling
{
    public class Panel_Graph
    {
        private int entryCount = 300;
        private bool showCalls = false;
        private bool showTimes = true;
        private string hoverString = string.Empty;
        private int hoverIdx = -1;

        // value = 0 - time, 1 - calls, 2 - background
        public static void DisplayColorPicker(Rect rect, int value)
        {
            Color32 col = new Color32();
            if (value == 0) col = Modbase.Settings.timeColour;
            else if (value == 1) col = Modbase.Settings.callsColour;
            else col = Modbase.Settings.GraphCol;

            Widgets.DrawBoxSolid(rect, col);

            if (!Widgets.ButtonInvisible(rect, true)) return;

            if (Find.WindowStack.WindowOfType<colourPicker>() != null)
            {
                Find.WindowStack.RemoveWindowsOfType(typeof(colourPicker));
                return;
            }

            colourPicker cp = new colourPicker();
            if (value == 0) cp.Setcol = () => Modbase.Settings.timeColour = colourPicker.CurrentCol;
            else if (value == 1) cp.Setcol = () => Modbase.Settings.callsColour = colourPicker.CurrentCol;
            else cp.Setcol = () => Modbase.Settings.GraphCol = colourPicker.CurrentCol;

            cp.SetColor(col);

            Find.WindowStack.Add(cp);
        }

        private static void DrawButton(Panel_Graph instance, Rect rect, string str, int idx)
        {
            var iconRect = rect.LeftPartPixels(20f);
            iconRect.height = 10;
            iconRect.x += 5;
            iconRect.width -= 10;
            iconRect.y += (rect.height / 2.0f) - 5f;
            rect.AdjustHorizonallyBy(20f);

            DisplayColorPicker(iconRect, idx);

            if (idx == 0 && !instance.showTimes) GUI.color = Color.grey;
            else if(idx == 1 && !instance.showCalls) GUI.color = Color.grey;
            
            Widgets.Label(rect, str);

            GUI.color = Color.white;

            if (idx == 2) return;

            if (Widgets.ButtonInvisible(rect))
            {
                if (idx == 0)
                {
                    instance.showTimes = !instance.showTimes;
                }
                else
                {
                    instance.showCalls = !instance.showCalls;
                }

            }

            Widgets.DrawHighlightIfMouseover(rect);
        }

        private static void DrawSettings(Panel_Graph instance, ref Rect position)
        {
            // [ - Calls ] [ - Times ] [ Lines ] [ Entries ------ ] [ - Bg Col ]

            var width = position.width;
            var currentHeight = 32;
            var currentSlice = position.TopPartPixels(currentHeight);
            position.AdjustVerticallyBy(currentHeight);

            Text.Anchor = TextAnchor.MiddleCenter;

            // [ - Times ]
            var str = " Times ";
            var contentWidth = 20 + str.GetWidthCached();
            var rect = currentSlice.LeftPartPixels(contentWidth);
            currentSlice.AdjustHorizonallyBy(contentWidth);

            DrawButton(instance, rect, " Times ", 0);

            // [ - Calls ]
            str = " Calls ";
            contentWidth = 20 + str.GetWidthCached();
            rect = currentSlice.LeftPartPixels(contentWidth);
            currentSlice.AdjustHorizonallyBy(contentWidth);
            
            DrawButton(instance, rect, " Calls ", 1);

            // [ - Background ]
            str = " Background ";
            contentWidth = 20 + str.GetWidthCached();
            rect = currentSlice.LeftPartPixels(contentWidth);
            currentSlice.AdjustHorizonallyBy(contentWidth);
            
            DrawButton(instance, rect, " Background ", 2);

            Text.Anchor = TextAnchor.UpperLeft;

            // [ - Entries ] 
            contentWidth = 150;
            if (currentSlice.width < contentWidth)
            {
                currentSlice = position.TopPartPixels(currentHeight);
                position.AdjustVerticallyBy(currentHeight);
            }

            rect = currentSlice.LeftPartPixels(contentWidth);
            instance.entryCount = (int)Widgets.HorizontalSlider(rect.BottomPartPixels(30f), instance.entryCount, 10, 2000, true, string.Intern($"{instance.entryCount} Entries  "));
            currentSlice.AdjustHorizonallyBy(contentWidth);


            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            

            // hits ... calls ... etc
            if (instance.hoverString != "")
            {
                str = instance.hoverString + "   ";
                contentWidth = str.GetWidthCached();
                if (currentSlice.width < contentWidth)
                {
                    currentSlice = position.TopPartPixels(currentHeight);
                    position.AdjustVerticallyBy(currentHeight);
                }

                rect = currentSlice.LeftPartPixels(contentWidth);
                Widgets.Label(rect, instance.hoverString);
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public void Draw(Rect rect)
        {
            DrawSettings(this, ref rect);

            if (Event.current.type != EventType.Repaint) return;

            DrawGraph(rect);
        }

        private void DrawGraph(Rect rect)
        {
            GUI.BeginGroup(rect);
            rect = rect.AtZero();

            Widgets.DrawBoxSolid(rect, Modbase.Settings.GraphCol);

            var entries = Mathf.Min(Analyzer.GetCurrentLogCount, entryCount);

            int i = entries;
            var prof = GUIController.CurrentProfiler;
            uint arrayIndex = prof.currentIndex;

            var calls = new List<int>(entries);
            var times = new List<float>(entries);

            var callsMax = 0;
            var timesMax = 0.0f;

            while (i > 0)
            {
                var timeEntry = (float)prof.times[arrayIndex];
                var hitsEntry = GUIController.CurrentEntry.type == typeof(H_HarmonyTranspilers) ? 0 : prof.hits[arrayIndex];

                calls.Add(hitsEntry);
                times.Add(timeEntry);

                if (callsMax < hitsEntry) callsMax = hitsEntry;
                if (timesMax < timeEntry) timesMax = timeEntry;

                i--;
                arrayIndex--;
                if (arrayIndex > Profiler.RECORDS_HELD) arrayIndex = Profiler.RECORDS_HELD - 1;
            }

            GraphDrawer.Draw(rect, timesMax, callsMax, entries, calls, times, showCalls, showTimes, ref hoverIdx, ref hoverString);

            GUI.EndGroup();
        }

        internal static class GraphDrawer
        {
            public static void Draw(Rect rect, float maxTime, int maxCalls, int entries, List<int> calls, List<float> times, bool showCalls, bool showTimes, ref int hoverIdx, ref string hoverStr)
            {
                var xIncrement = rect.width / (entries - 1.0f);

                var timeCutoff = (maxTime / rect.height) / 5.0f; // if the difference between two times is worth less than .5, merge the points together
                var callsCutoff = (maxCalls / rect.height) / 5.0f;

                int i = 1, timesIndex = 0, callsIndex = 0;

                for (; i < entries; i++)
                {
                    var currentX = i * xIncrement;

                    if (showCalls)
                    {
                        if (Mathf.Abs(calls[callsIndex] - calls[i]) > callsCutoff || i == entries - 1) // We need to draw a line, enough of a difference
                        {
                            var prevY = GetAdjustedY(calls[callsIndex], (float) maxCalls);
                            var nextY = GetAdjustedY(calls[i], (float) maxCalls);

                            if (callsIndex != i - 1)
                            {
                                // The first line should be straight so, lets use the one with fewer draw calls
                                Widgets.DrawLine(new Vector2(callsIndex * xIncrement, prevY), new Vector2((i - 1) * xIncrement, prevY), Modbase.Settings.callsColour, 1f);
                                //DubGUI.DrawLine(new Vector2(callsIndex * xIncrement, prevY), new Vector2((i - 1) * xIncrement, prevY), Modbase.Settings.callsColour, 1f, true);
                                DubGUI.DrawLine(new Vector2((i - 1) * xIncrement, prevY), new Vector2(currentX, nextY), Modbase.Settings.callsColour, 1f, true);
                            }
                            else 
                            {
                                DubGUI.DrawLine(new Vector2(callsIndex * xIncrement, prevY), new Vector2(currentX, nextY), Modbase.Settings.callsColour, 1f, true);
                            }


                            callsIndex = i;
                        }
                    }
                    Text.Font = GameFont.Tiny;
                    Text.Anchor = TextAnchor.MiddleCenter;

                    if(showTimes)
                    {
                        if (Mathf.Abs(times[timesIndex] - times[i]) > timeCutoff || i == entries - 1)
                        {
                            var prevY = GetAdjustedY(times[timesIndex], maxTime); 
                            var nextY = GetAdjustedY(times[i], maxTime);

                            if (timesIndex != i - 1)
                            {
                                Widgets.DrawLine(new Vector2(timesIndex * xIncrement, prevY), new Vector2((i - 1) * xIncrement, prevY), Modbase.Settings.timeColour, 1f);
                                //DubGUI.DrawLine(new Vector2(timesIndex * xIncrement, prevY), new Vector2(i - 1 * xIncrement, nextY), Modbase.Settings.timeColour, 1f, true);
                                DubGUI.DrawLine(new Vector2((i - 1) * xIncrement, prevY), new Vector2(currentX, nextY), Modbase.Settings.timeColour, 1f, true);
                            }
                            else
                            {
                                DubGUI.DrawLine(new Vector2(timesIndex * xIncrement, prevY), new Vector2(currentX, nextY), Modbase.Settings.timeColour, 1f, true);
                            }

                            timesIndex = i;
                        }
                    }

                    Rect relevantArea = new Rect(currentX - (xIncrement / 2f), 0, xIncrement, rect.height);
                        
                    if (Mouse.IsOver(relevantArea))
                    {
                        if (hoverIdx != i)
                        {
                            hoverStr = $"   {times[i]:0.00000}ms {calls[i]} call";
                            if (calls[i] > 1) hoverStr += "s";

                            hoverIdx = i;
                        }

                        SimpleCurveDrawer.DrawPoint(new Vector2( xIncrement * hoverIdx, 0));
                    }
                }
                
                float GetAdjustedY(float y, float max)
                {
                    return rect.height - (rect.height * (y / max));
                }

                Text.Anchor = TextAnchor.UpperLeft;
            }
        }
    }
}
