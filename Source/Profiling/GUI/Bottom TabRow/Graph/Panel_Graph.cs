using System;
using ColourPicker;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    internal struct GraphEntry
    {
        public float max;
        public float absMax;
        public bool visible;
        public float[] entries;
    }

    internal class GraphSettings
    {
        public static bool showAxis;
        public static bool showGrid;
        public static bool showMax = true;
        public static float lineAliasing; // Tweak if lines are merging too aggressively
        public Vector2 dragAnchor = new Vector2();


        public bool dragging = false;
        public Vector2 offsets = new Vector2(0, 0);
    }


    public class Panel_Graph
    {
        private static bool doSettings;
        internal GraphEntry calls = new GraphEntry { entries = new float[Profiler.RECORDS_HELD] };

        private int entryCount = 300;
        internal GraphSettings settings = new GraphSettings();
        internal GraphEntry times = new GraphEntry { entries = new float[Profiler.RECORDS_HELD], visible = true };


        public void Draw(Rect rect)
        {
            if (GUIController.CurrentProfiler == null) return;

            var c = new Rect(rect.x, rect.y, 20, 20);

            if (doSettings)
            {
                c.height = 30;
                DrawSettings(this, ref rect);
            }

            if (Mouse.IsOver(rect) && Event.current.isScrollWheel &&
                (Input.mouseScrollDelta.y > 0f || Input.mouseScrollDelta.y < 0f))
            {
                entryCount = Mathf.Clamp(entryCount - (int)(Input.mouseScrollDelta.y * 100), 100, 2000);
            }

            var stub = Vector2.right;
            Widgets.BeginScrollView(rect, ref stub, rect, false);
            var count = SetupArrays();
            GraphDrawer.DrawGraph(this, rect, count);
            Widgets.EndScrollView();

            if (!doSettings)
            {
                if (Widgets.ButtonImageFitted(c, Textures.Menu))
                {
                    doSettings = !doSettings;
                }
                c.x = c.xMax;
                if (Widgets.ButtonImageFitted(c, TexButton.SpeedButtonTextures[Analyzer.CurrentlyPaused ? 1 : 0]))
                {
                    Analyzer.CurrentlyPaused = !Analyzer.CurrentlyPaused;
                    GUIController.CurrentEntry.SetActive(!Analyzer.CurrentlyPaused);
                }
            }
        }



        public static void DisplayColorPicker(Rect rect, Color setcol, Action ac)
        {
            Widgets.DrawBoxSolid(rect, setcol);
            GUI.color = Color.grey;
            Widgets.DrawBox(rect);
            GUI.color = Color.white;
            if (!Widgets.ButtonInvisible(rect)) return;
            if (Find.WindowStack.WindowOfType<colourPicker>() != null)
            {
                Find.WindowStack.RemoveWindowsOfType(typeof(colourPicker));
                return;
            }

            var cp = new colourPicker();
            cp.SetColor(setcol);
            cp.Setcol = ac;
            Find.WindowStack.Add(cp);
        }

        private static bool ToggleColCombo(Rect rect, string str, bool enabled, Color setcol, Action ac)
        {
            rect.y += 4;
            rect.height -= 8;

            GUI.color = Color.grey * 0.7f;
            Widgets.DrawBox(rect);
            GUI.color = Color.white;

            var iconRect = rect.LeftPartPixels(20f);
            iconRect.height = 10;
            iconRect.x += 5;
            iconRect.width -= 10;
            iconRect.y += rect.height / 2.0f - 5f;
            rect.AdjustHorizonallyBy(20f);
            DisplayColorPicker(iconRect, setcol, ac);
            return DrawButton(rect, str, enabled, false);
        }

        private static bool DrawButton(Rect rect, string str, bool enabled, bool doborder = true)
        {
            if (doborder)
            {
                rect.y += 4;
                rect.height -= 8;

                GUI.color = Color.grey*0.7f;
                Widgets.DrawBox(rect);
                GUI.color = Color.white;
            }

            Text.Font = GameFont.Tiny;

            if (!enabled) GUI.color = Color.grey;

            Widgets.Label(rect, str);

            GUI.color = Color.white;

            Widgets.DrawHighlightIfMouseover(rect);
            Text.Font = GameFont.Small;

            if (Widgets.ButtonInvisible(rect))
            {
                return !enabled;
            }

            return enabled;
        }

        private static void DrawSettings(Panel_Graph instance, ref Rect position)
        {
            void CheckNewRow(ref Rect box, ref Rect position)
            {
                if (box.xMax > position.xMax)
                {
                    position.AdjustVerticallyBy(box.height);
                    box.y += box.height;
                    box.x = position.x;
                }
            }

            var currentHeight = 32;
            var box = position.TopPartPixels(currentHeight).LeftPartPixels(20f);
            position.AdjustVerticallyBy(currentHeight);

            Text.Anchor = TextAnchor.MiddleCenter;

            if (Widgets.ButtonImageFitted(box, Textures.Menu))
            {
                doSettings = !doSettings;
            }
            box.ShiftX(5);

            if (Widgets.ButtonImageFitted(box, TexButton.SpeedButtonTextures[Analyzer.CurrentlyPaused ? 1 : 0]))
            {
                Analyzer.CurrentlyPaused = !Analyzer.CurrentlyPaused;
                GUIController.CurrentEntry.SetActive(!Analyzer.CurrentlyPaused);
            }
            box.ShiftX(5);

            var str = "Times";
            box.width = 20 + str.GetWidthCached();

            instance.times.visible = ToggleColCombo(box, str, instance.times.visible, Settings.timeColour, () => Settings.timeColour = colourPicker.CurrentCol);

            box.ShiftX(5);

            str = "Calls";
            box.width = 20 + str.GetWidthCached();

            instance.calls.visible = ToggleColCombo(box, str, instance.calls.visible, Settings.callsColour, () => Settings.callsColour = colourPicker.CurrentCol);

            box.ShiftX(5);

            str = "Background";
            box.width = 20 + str.GetWidthCached();

            ToggleColCombo(box, str, true, Settings.GraphCol, () => Settings.GraphCol = colourPicker.CurrentCol);

            box.ShiftX(5);

            void jammydodger(ref Rect p, string s, ref bool r)
            {
                box.width = 20 + s.GetWidthCached();
                CheckNewRow(ref box, ref p);

                r = DrawButton(box, s, r);
                box.ShiftX(5);
            }

            jammydodger(ref position, "Axis", ref GraphSettings.showAxis);
            jammydodger(ref position, "Grid", ref GraphSettings.showGrid);
            jammydodger(ref position, "Max", ref GraphSettings.showMax);

            Text.Anchor = TextAnchor.UpperLeft;

            box.width = 100;
            CheckNewRow(ref box, ref position);

            instance.entryCount = (int)Widgets.HorizontalSlider(box.BottomPartPixels(30f), instance.entryCount, 10, 2000, true, string.Intern($"{instance.entryCount} Entries"));

            box.ShiftX(5);


            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            str = $"Aliasing:{(GraphSettings.lineAliasing == 0 ? "none" : GraphSettings.lineAliasing.ToString())}";
            box.width = str.GetWidthCached() + 10;
            CheckNewRow(ref box, ref position);

            if (Widgets.ButtonText(box, str, false))
            {
                GraphSettings.lineAliasing = GraphSettings.lineAliasing switch
                {
                    7.5f => 12.5f,
                    12.5f => 0.0f,
                    0.0f => 5.0f,
                    5.0f => 7.5f,
                    _ => 0.0f
                };
            }

            box.ShiftX(5);

            DubGUI.ResetFont();
        }

        internal int SetupArrays()
        {
            var entries = Mathf.Min(Analyzer.GetCurrentLogCount, entryCount);
            settings.offsets.x = Mathf.Clamp(settings.offsets.x, 0, entryCount);


            var i = entries;
            var prof = GUIController.CurrentProfiler;
            var arrayIndex = prof.currentIndex;

            // arrayIndex = 300
            // offset = 400
            // correct starting position = (RecordsHeld) - (offset - arrayIndex)

            // arrayIndex = 1700
            // offset = 1600
            // correct starting position = arrayIndex - offset

            if (arrayIndex < settings.offsets.x)
                arrayIndex = Profiler.RECORDS_HELD - ((uint)settings.offsets.x - arrayIndex);
            else arrayIndex -= (uint)settings.offsets.x;


            var callsMax = 0;
            var timesMax = 0.0f;

            while (i > 0)
            {
                var timeEntry = (float)prof.times[arrayIndex];
                var hitsEntry = GUIController.CurrentEntry.type == typeof(H_HarmonyTranspilersInternalMethods)
                    ? 0
                    : prof.hits[arrayIndex];

                calls.entries[i - 1] = hitsEntry;
                times.entries[i - 1] = timeEntry;

                if (callsMax < hitsEntry) callsMax = hitsEntry;
                if (timesMax < timeEntry) timesMax = timeEntry;

                i--;
                arrayIndex--;
                if (arrayIndex > Profiler.RECORDS_HELD) arrayIndex = Profiler.RECORDS_HELD - 1;
            }

            if (calls.max > callsMax) calls.max -= (calls.max - callsMax) / 120.0f;
            else calls.max = callsMax;

            calls.absMax = callsMax;

            if (times.max > timesMax) times.max -= (times.max - timesMax) / 120.0f;
            else times.max = timesMax;

            times.absMax = timesMax;

            return entries;
        }
    }
}