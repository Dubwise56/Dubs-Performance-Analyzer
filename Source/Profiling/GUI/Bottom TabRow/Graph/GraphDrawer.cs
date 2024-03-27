using System;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    public static class GraphDrawer
    {
        private static string hoverValStr = string.Empty;
        private static float visibleMin = 0;

        public static Color gray = new Color(0.5f, 0.5f, 0.5f, .7f);

        public static void DrawGraph(Panel_Graph instance, Rect rect, int entries)
        {
            var statline = rect.TopPartPixels(20f);
            rect.AdjustVerticallyBy(20f);
            Widgets.DrawRectFast(statline, Settings.GraphCol);
            Widgets.DrawRectFast(rect, Settings.GraphCol);


            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(statline, hoverValStr);

            var primaryEntry = instance.times.visible ? instance.times : instance.calls;
            var suffix = instance.times.visible ? "ms" : "calls";
            var maxminStr = $"Max:{primaryEntry.absMax:0.0000}{suffix}";
            if (Analyzer.CurrentlyPaused || Find.TickManager.Paused)
            {
                maxminStr = $"Max:{primaryEntry.absMax:0.0000}{suffix} Min:{visibleMin:0.0000}{suffix}";
            }
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(statline, maxminStr);
            Text.Anchor = TextAnchor.UpperLeft;

            GUI.BeginGroup(rect);
            rect = rect.AtZero();

            AdjustDimensions(instance, rect, entries);

            if (GraphSettings.showAxis)
            {
                var axis = rect;
                axis.height -= Text.LineHeight;
                DrawAxis(axis, primaryEntry.max, instance.times.visible ? "ms" : "calls");

                rect.x += 25;
                rect.width -= 25;

                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.LowerLeft;
                int xAxisValue = Mathf.CeilToInt(entries + (int)instance.settings.offsets.x);
                Widgets.Label(rect, Mathf.CeilToInt(xAxisValue).ToString());
                Text.Anchor = TextAnchor.LowerCenter;
                xAxisValue = Mathf.CeilToInt((entries / 2f) + (int)instance.settings.offsets.x);
                Widgets.Label(rect, Mathf.CeilToInt(xAxisValue).ToString());
                Text.Anchor = TextAnchor.LowerRight;
                xAxisValue = Mathf.CeilToInt((int)instance.settings.offsets.x);
                Widgets.Label(rect, Mathf.CeilToInt(xAxisValue).ToString());
                Text.Anchor = TextAnchor.UpperLeft;

                rect.height -= Text.LineHeight;

                DrawGrid(rect);

                GUI.BeginGroup(rect);
                rect = rect.AtZero();
            }



            if (GraphSettings.showMax)
            {
                DrawMaxLine(rect, primaryEntry.absMax, primaryEntry.max, suffix);
            }

            DrawEntries(rect, instance, entries);

            if (GraphSettings.showAxis) GUI.EndGroup();

            GUI.EndGroup();
        }

        internal static void DrawEntries(Rect rect, Panel_Graph instance, int entries)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            var xIncrement = rect.width / (entries - 1.0f);

            var timeCutoff = 0.0f;
            var callsCutoff = 0.0f;

            int i = 1, timesIndex = 0, callsIndex = 0;

            visibleMin = instance.times.entries[timesIndex];
            for (; i < entries; i++)
            {
                if (instance.calls.visible)
                {
                    if (Mathf.Abs(instance.calls.entries[callsIndex] - instance.calls.entries[i]) > callsCutoff || i == entries - 1) // We need to draw a line, enough of a difference
                    {
                        DrawLine(instance.calls, callsIndex, i, rect.height, xIncrement, Settings.callsColour);

                        callsIndex = i;
                    }
                }

                if (!instance.times.visible) continue;

                if (Mathf.Abs(instance.times.entries[timesIndex] - instance.times.entries[i]) > timeCutoff || i == entries - 1)
                {
                    var point = DrawLine(instance.times, timesIndex, i, rect.height, xIncrement, Settings.timeColour);

                    if (instance.times.entries[i] < visibleMin)
                    {
                        visibleMin = instance.times.entries[timesIndex];
                    }

                    var cheese = new Rect(point.x - (xIncrement * 0.5f), 0, xIncrement, rect.height);
                    if (Mouse.IsOver(cheese))
                    {
                        SimpleCurveDrawer.DrawPoint(point);
                        hoverValStr = $"{instance.times.entries[timesIndex]}ms {instance.calls.entries[timesIndex]} calls";
                    }

                    timesIndex = i;
                }
            }
        }


        internal static Vector2 DrawLine(GraphEntry value, int prevIndex, int nextIndex, float rectHeight, float xIncrement, Color color)
        {
            float GetAdjustedY(float y, float max)
            {
                return rectHeight - rectHeight * .95f * (y / max);
            }

            var prevY = GetAdjustedY(value.entries[prevIndex], value.max);
            var nextY = GetAdjustedY(value.entries[nextIndex], value.max);

            if (prevIndex != nextIndex - 1) // We have aliased a point (or multiple) we need to draw two lines.
            {
                var prevDrawnPoint = new Vector2(prevIndex * xIncrement, prevY);
                var prevPoint = new Vector2((nextIndex - 1) * xIncrement, prevY);
                var curPoint = new Vector2(nextIndex * xIncrement, nextY);
                DubGUI.DrawLine(prevDrawnPoint, prevPoint, color, 1f, true);
                DubGUI.DrawLine(prevPoint, curPoint, color, 1f, true);
            }
            else
            {
                DubGUI.DrawLine(new Vector2(prevIndex * xIncrement, prevY), new Vector2(nextIndex * xIncrement, nextY), color, 1f, true);
            }

            return new Vector2(prevIndex * xIncrement, prevY);
        }

        internal static void AdjustDimensions(Panel_Graph instance, Rect rect, int entries)
        {
            if (Input.GetMouseButtonDown(0) && Mouse.IsOver(rect) && !instance.settings.dragging)
            {
                instance.settings.dragging = true;
                instance.settings.dragAnchor = Event.current.mousePosition;
            }

            if (instance.settings.dragging)
            {
                var mousePos = Event.current.mousePosition;

                var deltaX = mousePos.x - instance.settings.dragAnchor.x;

                if (Mathf.Abs(deltaX) > 1)
                {
                    instance.settings.offsets.x += deltaX;
                    instance.settings.offsets.x = Mathf.Clamp(instance.settings.offsets.x, 0, Analyzer.GetCurrentLogCount - entries - 1);

                    instance.settings.dragAnchor.x = mousePos.x;
                }
            }

            if (Input.GetMouseButtonUp(0)) instance.settings.dragging = false;
        }

        internal static void DrawMaxLine(Rect rect, float absMax, float maxValue, string suffix)
        {
            var y = rect.height - rect.height * .95f * (absMax / maxValue);

            var col = GUI.color;
            GUI.color = Color.red * .75f;
            Widgets.DrawLineHorizontal(0, y, rect.width);
            GUI.color = col;
        }

        internal static void DrawGrid(Rect rect)
        {
            if (Event.current.type != EventType.Repaint) return;

            var yIncrement = rect.height / 4f;

            for (var i = 0; i < 5; i++)
            {
                Widgets.DrawLine(new Vector2(rect.x, i * yIncrement), new Vector2(GraphSettings.showGrid ? rect.xMax : rect.x + 2f, i * yIncrement), gray, 1f);
            }
        }

        internal static void DrawAxis(Rect rect, float yAxis, string suffix)
        {
            var yIncrement = rect.height / 4f;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(rect, suffix);
            for (var i = 1; i < 5; i++)
            {
                var box = new Rect(0, 0, 25f, Text.LineHeight);
                box.y = (i * yIncrement) - (box.height / 2f);

                Widgets.Label(box, Mathf.Round((4 - i) * (yAxis / 4f) * 100) / 100 + "");

            }
            DubGUI.ResetFont();
        }
    }
}