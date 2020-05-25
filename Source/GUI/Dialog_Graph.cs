using ColourPicker;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace DubsAnalyzer
{

    [StaticConstructorOnStartup]
    public static class Dialog_Graph
    {
      //  public static Texture2D mem = ContentFinder<Texture2D>.Get("DPA/UI/mem", false);

        private static int entryCount = 300;
        public static string key = string.Empty;

        private static Vector2 last = Vector2.zero;
        private static Vector2 lastMEM = Vector2.zero;
        private static int hoverVal;
        private static string hoverValStr = string.Empty;
        private static int ResetRange;

        private static float WindowMax;

        private static double max;
        private static string MaxStr;
        private static string totalBytesStr;

        public static void RunKey(string s)
        {
            reset();
            key = s;
        }

        public static void reset()
        {
            WindowMax = 0;
            max = 0;
            totalBytesStr = string.Empty;
            key = string.Empty;
            hoverValStr = string.Empty;
            MaxStr = string.Empty;
            lastMEM = Vector2.zero;
            last = Vector2.zero;
        }

        public static void DoGraph(Rect position)
        {
            ResetRange++;
            if (ResetRange >= 500)
            {
                ResetRange = 0;
                WindowMax = 0;
            }

            Text.Font = GameFont.Small;

            var settings = position.TopPartPixels(30f);
            position = position.BottomPartPixels(position.height - 30f);

            Widgets.DrawBoxSolid(position, Analyzer.Settings.GraphCol);

            GUI.color = Color.grey;
            Widgets.DrawBox(position, 2);
            GUI.color = Color.white;

            if (!Analyzer.Profiles.ContainsKey(key)) return;

            var prof = Analyzer.Profiles[key];

            if (prof.History.times.Length <= 0) return;

            var mescou = prof.History.times.Length;

            if (mescou > entryCount)
                mescou = entryCount;

            var gap = position.width / mescou;

            var car = settings.RightPartPixels(200f);
            car.x -= 15;
            entryCount = (int)Widgets.HorizontalSlider(car, entryCount, 10, 2000, true, string.Intern($"{entryCount} Entries"));

            car = new Rect(car.xMax + 5, car.y + 2, 10, 10);
            Widgets.DrawBoxSolid(car, Analyzer.Settings.LineCol);
            if (Widgets.ButtonInvisible(car, true))
            {
                if (Find.WindowStack.WindowOfType<colourPicker>() != null)
                {
                    Find.WindowStack.RemoveWindowsOfType(typeof(colourPicker));
                }
                else
                {
                    var cp = new colourPicker
                    {
                        Setcol = () => Analyzer.Settings.LineCol = colourPicker.CurrentCol
                    };
                    cp.SetColor(Analyzer.Settings.LineCol);
                    Find.WindowStack.Add(cp);
                }
            }
            car.y += 12;
            Widgets.DrawBoxSolid(car, Analyzer.Settings.GraphCol);
            if (Widgets.ButtonInvisible(car, true))
            {
                if (Find.WindowStack.WindowOfType<colourPicker>() != null)
                {
                    Find.WindowStack.RemoveWindowsOfType(typeof(colourPicker));
                }
                else
                {
                    var cp = new colourPicker
                    {
                        Setcol = () => Analyzer.Settings.GraphCol = colourPicker.CurrentCol
                    };
                    cp.SetColor(Analyzer.Settings.GraphCol);
                    Find.WindowStack.Add(cp);
                }
            }

            if (Analyzer.Settings.AdvancedMode)
            {
                var memr = settings.LeftPartPixels(20f);
            //    if (Widgets.ButtonImageFitted(memr, mem, ShowMem ? Color.white : Color.grey))
            //    {
            //        ShowMem = !ShowMem;
            //    }
                GUI.color = Color.white;
                TooltipHandler.TipRegion(memr, "Toggle garbage tracking, approximation of total garbage produced by the selected log");

                memr.x = memr.xMax;
                memr.width = 300f;
             //   if (ShowMem)
            //    {
            //        Text.Anchor = TextAnchor.MiddleLeft;
           ////         Widgets.Label(memr, totalBytesStr);
           ///     }
            }


            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(settings, hoverValStr);
            Text.Anchor = TextAnchor.UpperLeft;

         //   maxBytes = 0;
          //  minBytes = 0;

            var LastMax = max;
            max = 0;

            GUI.BeginGroup(position);
            position = position.AtZero();

            for (var i = 0; i < mescou; i++)
            {
              //  var bytes = prof.History.mem[i];
                var TM = prof.History.times[i];

                if (i == 0)
                {
              //      minBytes = bytes;
              //      maxBytes = bytes;
                    max = TM;
                }

             //   if (bytes < minBytes)
             //   {
             //       minBytes = bytes;
              //  }

            //    if (bytes > maxBytes)
           //     {
           //         maxBytes = bytes;
           //     }

                if (TM > max)
                {
                    max = TM;
                }
            }

            if (max > WindowMax)
            {

                WindowMax = (float)max;
            }

#pragma warning disable CS0219 
            var DoHover = false;
#pragma warning restore CS0219 

            for (var i = 0; i < mescou; i++)
            {
            //    var bytes = prof.History.mem[i];
                float TM = (float)prof.History.times[i];

                var y = GenMath.LerpDoubleClamped(0, WindowMax, position.height, position.y, (float)TM);
              //  var MEMy = GenMath.LerpDoubleClamped(minBytes, maxBytes, position.height, position.y, bytes);

                var screenPoint = new Vector2(position.xMax - gap * i, y);
            //    var MEMscreenPoint = new Vector2(position.xMax - gap * i, MEMy);

                if (i != 0)
                {
                    Widgets.DrawLine(last, screenPoint, Analyzer.Settings.LineCol, 1f);
                 //   if (ShowMem)
                 //   {
                 //       Widgets.DrawLine(lastMEM, MEMscreenPoint, Color.grey, 2f);
                 //   }

                    var vag = new Rect(screenPoint.x - gap / 2f, position.y, gap, position.height);

                    //if (Widgets.ButtonInvisible(vag))
                    //{
                    //    Log.Warning(prof.History.stack[i]);
                    //    Find.WindowStack.Windows.Add(new StackWindow { stkRef = prof.History.stack[i] });
                    //}

                    if (Mouse.IsOver(vag))
                    {
                        DoHover = true;
                        if (i != hoverVal)
                        {
                            hoverVal = i;
                            hoverValStr = $"{TM} {prof.History.hits[i]} calls";
                        }
                        SimpleCurveDrawer.DrawPoint(screenPoint);
                    }
                }

                last = screenPoint;
            //    lastMEM = MEMscreenPoint;
            }

            if (LastMax != max)
            {
                MaxStr = $"Max {max}ms";
            }

         //   if (LASTtotalBytesStr < prof.BytesUsed)
         ///   {
         //       LASTtotalBytesStr = prof.BytesUsed;
         //       totalBytesStr = $"Mem {(long)(prof.BytesUsed / (long)1024)} Kb";
         //   }


            var LogMaxY = GenMath.LerpDoubleClamped(0, WindowMax, position.height, position.y, (float)max);
            var crunt = position;
            crunt.y = LogMaxY;
            Widgets.Label(crunt, MaxStr);
            Widgets.DrawLine(new Vector2(position.x, LogMaxY), new Vector2(position.xMax, LogMaxY), Color.red, 1f);

            last = Vector2.zero;
        }
    }
}