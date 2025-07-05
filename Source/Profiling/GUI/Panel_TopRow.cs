using System;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    public static class Panel_TopRow
    {
        public static string TimesFilter = string.Empty;
        public static string MatchType = string.Empty;

        public static int
            tps,
            tpsTarget,
            fps;

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

            var searchbox = rect.LeftPartPixels(rect.width - 300f);
            searchbox.x += 25f;

            DubGUI.InputField(searchbox, Strings.top_search, ref TimesFilter, DubGUI.MintSearch);

            rect.AdjustHorizonallyBy(rect.width - 250f);
            
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Tiny;
            
            var cat = GUIController.CurrentCategory == Category.Tick ? Strings.tab_tick : Strings.Frame;
            var str = $"{ProfileController.updateAverage:F3}ms/{cat}";

            var strLen = str.GetWidthCached();

            var periodLen = rect.LeftPartPixels(130);
            rect.AdjustHorizonallyBy(130);
            
            Widgets.Label(periodLen, str);

            if (Analyzer.CurrentlyProfiling)
            {
                fps = GUIElement_TPS.FPS;
                tps = GUIElement_TPS.TPS;
                tpsTarget = GUIElement_TPS.TPSTarget;
            }

            var tpsFpsRect = rect;
            tpsFpsRect.width = 50f;
            Widgets.Label(tpsFpsRect, $"FPS: {fps}");
            TooltipHandler.TipRegion(tpsFpsRect, Strings.top_fps_tip);
            tpsFpsRect.x = tpsFpsRect.xMax + 5;
            tpsFpsRect.width = 90f;
            Widgets.Label(tpsFpsRect, $"TPS: {tps}({tpsTarget})");
            TooltipHandler.TipRegion(tpsFpsRect, Strings.top_tps_tip);
            tpsFpsRect.x = tpsFpsRect.xMax + 5;
            tpsFpsRect.width = 30f;
            Text.Font = GameFont.Medium;
        }
    }
}