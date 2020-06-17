using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace DubsAnalyzer
{
    public static class DropDownSearchMenu
    {
        private static float yHeigthCache = 9999999;
        private static Vector2 searchpos = Vector2.zero;
        public static Listing_Standard listing = new Listing_Standard();

        public static void DoWindowContents(Rect rect)
        {
            rect.height = 2000;

            var baseRect = rect.AtZero();
            baseRect.y += Text.LineHeight;
            baseRect.height = yHeigthCache;

            Widgets.BeginScrollView(rect, ref searchpos, baseRect, false);
            GUI.BeginGroup(baseRect);
            listing.Begin(baseRect);

            float yHeight = 0;

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Tiny;

            var count = 0;
            foreach (var entry in PerfAnalSettings.cachedEntries)
            {
                count++;
                if (count == 50)
                {
                    break;
                }

                var r = listing.GetRect(Text.LineHeight);

                if (Widgets.ButtonInvisible(r))
                    PerfAnalSettings.currentInput = entry;

                Widgets.DrawBoxSolid(r, Analyzer.Settings.GraphCol);

                r.width = 2000;
                Widgets.Label(r, " " + entry);
                listing.GapLine(0f);
                yHeight += 4f;
                yHeight += r.height;
            }

            listing.End();
            yHeigthCache = yHeight;
            GUI.EndGroup();

            DubGUI.ResetFont();
            Widgets.EndScrollView();
        }
    }
}
