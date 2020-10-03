﻿using ColourPicker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;

namespace Analyzer.Profiling
{


    public static class Panel_Graph
    {
        private static int entryCount = 300;
        private static int hoverVal;
        private static string hoverValStr = string.Empty;
        private static int ResetRange;

        private static float WindowMax;
        private static bool showTime = false;
        private static double max;
        private static string MaxStr;
        public static int Entries = 0;

        public static void DisplayColorPicker(Rect rect, bool LineCol)
        {
            Widgets.DrawBoxSolid(rect, (LineCol) ? Modbase.Settings.LineCol : Modbase.Settings.GraphCol);
             
            if (Widgets.ButtonInvisible(rect, true))
            {
                if (Find.WindowStack.WindowOfType<colourPicker>() != null) // if we already have a colour window open, close it
                    Find.WindowStack.RemoveWindowsOfType(typeof(colourPicker));

                else
                {
                    colourPicker cp = new colourPicker();
                    if (LineCol) cp.Setcol = () => Modbase.Settings.LineCol = colourPicker.CurrentCol;
                    else cp.Setcol = () => Modbase.Settings.GraphCol = colourPicker.CurrentCol;

                    cp.SetColor((LineCol) ? Modbase.Settings.LineCol : Modbase.Settings.GraphCol);

                    Find.WindowStack.Add(cp);
                }
            }
        }

        public static void DrawSettings(Rect rect)
        {
            Rect sliderRect = rect.RightPartPixels(200f).BottomPartPixels(30f);
            sliderRect.x -= 15;
            entryCount = (int)Widgets.HorizontalSlider(sliderRect, entryCount, 10, 2000, true, string.Intern($"{entryCount} Entries"));
            sliderRect = new Rect(sliderRect.xMax + 5, sliderRect.y + 2, 10, 10);

            DisplayColorPicker(sliderRect, true);
            sliderRect.y += 12;
            DisplayColorPicker(sliderRect, false);

            if (showTime)
            {
                rect.width -= 220;
                Text.Anchor = TextAnchor.LowerRight;
                Widgets.Label(rect, hoverValStr);
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        public static void Draw(Rect r)
        {
            var position = r;
            ResetRange++;
            if (ResetRange >= 500)
            {
                ResetRange = 0;
                WindowMax = 0;
            }

            Text.Font = GameFont.Small;

            Profiler prof = GUIController.CurrentProfiler;
            if (prof == null) return;

            int entries = Mathf.Min(Analyzer.GetCurrentLogCount, entryCount);


            var TopBox = position.TopPartPixels(60f).ContractedBy(2f);
            Panel_Details.DrawMethDeets(TopBox);
            DrawSettings(TopBox);
            position = position.BottomPartPixels(position.height - TopBox.height);

            Widgets.DrawBoxSolid(position, Modbase.Settings.GraphCol);

            GUI.color = Color.grey;
            Widgets.DrawBox(position, 2);
            GUI.color = Color.white;

            float gap = position.width / entries;

            GUI.BeginGroup(position);
            {
                position = position.AtZero();

                double LastMax = max;
                var log = Analyzer.Logs.First(log => log.key == prof.key);
                max = log.max;
                var maxCalls = log.maxCalls;

                if (max > WindowMax)
                    WindowMax = (float)max;

                int counter = entries;
                uint profIndex = prof.currentIndex;

                var diff = position.y - position.height;
                var av = log.average;

                Vector2 last = new Vector2();
                //Vector2 lastHits = new Vector2();

                //Material mat = Widgets.LineMat;
                //Widgets.LineTexAA.filterMode = FilterMode.Trilinear;
                //GL.PushMatrix();

                //mat.SetPass(0);
                //GL.LoadPixelMatrix();

                //GL.Begin(GL.LINES);

                //GL.Color(Color.red);

                showTime = false;
                while (counter > 0)
                {
                    var adjIndex = entries - counter;
                    var timeEntry = (float)prof.times[profIndex];
                    var hitsEntry = prof.hits[profIndex];

                    var y = position.height + (diff) * (timeEntry / WindowMax);
                    //var callsY = position.height + (diff) * (hitsEntry / maxCalls);

                    Vector2 screenPoint = new Vector2(position.xMax - (gap * adjIndex), y);
                    //Vector2 hitsPoint = new Vector2(position.xMax - (gap * adjIndex), callsY);

                    if (adjIndex != 0)
                    {
                        DubGUI.DrawLine(last, screenPoint, Modbase.Settings.LineCol, 1f);
                        DubGUI.DrawLine(last, screenPoint, Modbase.Settings.LineCol, 1f);
                        //Widgets.DrawLine(lastHits, hitsPoint, Color.yellow, 1f);

                        Rect relevantArea = new Rect(screenPoint.x - gap / 2f, position.y, gap, position.height);
                        
                        if (Mouse.IsOver(relevantArea))
                        {
                            showTime = true;
                            if (adjIndex != hoverVal)
                            {
                                hoverVal = adjIndex;
                                if (hitsEntry == 1)
                                {
                                    hoverValStr = $"{timeEntry:0.00000}ms {hitsEntry} call";
                                }
                                else
                                {
                                    hoverValStr = $"{timeEntry:0.00000}ms {hitsEntry} calls";
                                }
                            }
                            SimpleCurveDrawer.DrawPoint(screenPoint);
                            //var flotrec = new Rect(0, 0, 300, 30);
                            //Vector2 vector = UI.MousePositionOnUIInverted + new Vector2(0f, -30f);
                            //if (vector.x + flotrec.width > (float)UI.screenWidth)
                            //{
                            //    vector.x = (float)UI.screenWidth - flotrec.width;
                            //}
                            //if (vector.y + flotrec.height > (float)UI.screenHeight)
                            //{
                            //    vector.y = (float)UI.screenHeight - flotrec.height;
                            //}

                            //flotrec.position = vector;

                            //flotrec.x -= flotrec.width / 2f;

                            //void plonkit()
                            //{
                            //    var r1 = flotrec.AtZero();
                            //    Text.Anchor = TextAnchor.MiddleCenter;
                            //    Widgets.Label(r1, hoverValStr);
                            //    Text.Anchor = TextAnchor.UpperLeft;
                            //}
                            //Find.WindowStack.ImmediateWindow(172457, flotrec, WindowLayer.Super, plonkit, false, false, 1f);
                        }
                    }


                    last = screenPoint;
                    //lastHits = hitsPoint;

                    counter--;
                    profIndex = (profIndex - 1) % Profiler.RECORDS_HELD;
                }

                //var averageY = (float)(position.height + (diff) * (av / WindowMax));

                //var start = new Vector2(position.x, averageY);
                //var end = new Vector2(position.x + position.width, averageY);

                //Widgets.DrawLine(start, end, Color.yellow, 1f);

                if (LastMax != max) MaxStr = $" Max: {max:0.0000}ms";

                float LogMaxY = GenMath.LerpDoubleClamped(0, WindowMax, position.height, position.y, (float)max);
                Rect crunt = position;
                crunt.y = LogMaxY;
                Widgets.Label(crunt, MaxStr); // $" Max Time: {max:0.0000}ms\nMax Calls: {maxCalls}");
                Widgets.DrawLine(new Vector2(position.x, LogMaxY), new Vector2(position.xMax, LogMaxY), Color.red, 1f);

                last = Vector2.zero;
            }
            GUI.EndGroup();

            Entries = entries;
        }
    }
}
