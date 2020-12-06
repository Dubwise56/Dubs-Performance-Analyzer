using System;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    public static class Panel_TopRow
    {
        public static string TimesFilter = string.Empty;
        public static string MatchType = string.Empty;

        public static void Draw(Rect rect)
        {
            var row = rect.LeftPartPixels(25f);

            if (Widgets.ButtonImage(row, TexButton.SpeedButtonTextures[Analyzer.CurrentlyPaused ? 1 : 0]))
            {
                Analyzer.CurrentlyPaused = !Analyzer.CurrentlyPaused;
                GUIController.CurrentEntry.SetActive(!Analyzer.CurrentlyPaused);
            }

            TooltipHandler.TipRegion(row, Strings.top_pause_analyzer);
            rect.AdjustHorizonallyBy(25f);

            row = rect.LeftPartPixels(25);
            if (Widgets.ButtonImage(row, Textures.refresh))
            {
                GUIController.ResetProfilers();
            }

            TooltipHandler.TipRegion(row, Strings.top_refresh);

            var searchbox = rect.LeftPartPixels(rect.width - 220f);
            searchbox.x += 25f;

            DubGUI.InputField(searchbox, Strings.top_search, ref TimesFilter, DubGUI.MintSearch);
        //    searchbox.x = searchbox.xMax;
        //    searchbox.width = 150;
         //   GUI.color = Color.grey;
         //   Widgets.Label(searchbox, MatchType);
         //   GUI.color = Color.white;


            // bit shitty and distracting, replace with a mini graph and or an entire page dedicated to garbage if it even matters realistically now which it probably doesn't so why bother aye just keep it clean
            //row.x = searchbox.xMax + 5;
            // row.width = 130f;
            //Text.Anchor = TextAnchor.MiddleCenter;
            //Widgets.FillableBar(row, Mathf.Clamp01(Mathf.InverseLerp(H_RootUpdate.LastMinGC, H_RootUpdate.LastMaxGC, H_RootUpdate.totalBytesOfMemoryUsed)), Textures.darkgrey);
            //Widgets.Label(row, H_RootUpdate.GarbageCollectionInfo);
            //TooltipHandler.TipRegion(row, Strings.top_gc_tip);
           
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Tiny;

            row.width = 50f;
            row.x = searchbox.xMax + 10;
            Widgets.Label(row, $"FPS: {GUIElement_TPS.FPS}");
            TooltipHandler.TipRegion(row, Strings.top_fps_tip);
            row.x = row.xMax + 5;
            row.width = 90f;
            Widgets.Label(row, $"TPS: {GUIElement_TPS.TPS}({GUIElement_TPS.TPSTarget})");
            TooltipHandler.TipRegion(row, Strings.top_tps_tip);
            row.x = row.xMax + 5;
            row.width = 30f;
            Text.Font = GameFont.Medium;
        }
    }
}