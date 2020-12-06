using System.Collections.Generic;
using Analyzer.Performance;
using Analyzer.Profiling;
using UnityEngine;
using Verse;

namespace Analyzer
{
    public static class Panel_Settings
    {
        public static Listing_Standard listing = new Listing_Standard();
        private static Vector2 scrollPos;
        private static int currentTab;
        private static float listheight = 999;

        public static void Draw(Rect rect, bool settingsPage = false)
        {
           
            rect = rect.Rounded();
            rect.height -= 32;
            rect.y += 32;
            Widgets.DrawMenuSection(rect);

       

            if (settingsPage)
            {
                currentTab = 0;
            }
            else
            {
                var list = new List<TabRecord>();
                list.Add(new TabRecord("settings.performance".Translate(), delegate
                {
                    currentTab = 0;
                    Modbase.Settings.Write();
                }, currentTab == 0));
                list.Add(new TabRecord("settings.developer".Translate(), delegate
                {
                    currentTab = 1;
                    Modbase.Settings.Write();
                }, currentTab == 1));

                TabDrawer.DrawTabs(rect, list);
            }

            rect = rect.ContractedBy(10);

            listing.maxOneColumn = true;
            var innyrek = new Rect(0, 0, rect.width - 16f, listheight);
            if (innyrek.width < 400)
            {
                innyrek.width = 400; 
            }
            Widgets.BeginScrollView(rect, ref scrollPos, innyrek);
         
            listing.Begin(innyrek);

            var rec = listing.GetRect(24f);
            var lrec = rec.LeftHalf();
            rec = rec.RightHalf();
            Widgets.DrawTextureFitted(lrec.LeftPartPixels(40f), Textures.Support, 1f);
            lrec.x += 40;
            if (Widgets.ButtonText(lrec.LeftPartPixels(Strings.settings_wiki.GetWidthCached()),
                Strings.settings_wiki, false))
            {
                Application.OpenURL("https://github.com/Dubwise56/Dubs-Performance-Analyzer/wiki");
            }

            Widgets.DrawTextureFitted(rec.RightPartPixels(40f), Textures.disco, 1f);
            rec.width -= 40;
            if (Widgets.ButtonText(rec.RightPartPixels(Strings.settings_discord.GetWidthCached()),
                Strings.settings_discord, false))
            {
                Application.OpenURL("https://discord.gg/Az5CnDW");
            }

            listing.GapLine();

            if (currentTab == 0)
            {
                PerformancePatches.Draw(listing);
            }
            else
            {
                Panel_DevOptions.Draw(listing, rect);
            }

            listheight = listing.curY;
            listing.End();
            Widgets.EndScrollView();
          
        }
    }
}