using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Analyzer
{
    public static class Panel_TopRow
    {
        public static string TimesFilter = string.Empty;

        public static void Draw(Rect rect)
        {
            Rect row = rect.LeftPartPixels(25f);

            if (Widgets.ButtonImage(row, TexButton.SpeedButtonTextures[Analyzer.CurrentlyPaused ? 1 : 0]))
            {
                Analyzer.CurrentlyPaused = !Analyzer.CurrentlyPaused;
                GUIController.CurrentEntry.SetActive(Analyzer.CurrentlyPaused);
            }

            TooltipHandler.TipRegion(row, ResourceCache.Strings.top_pause_analyzer);

            Rect searchbox = row.LeftPartPixels(row.width - 350f);
            searchbox.x += 25f;
            DubGUI.InputField(searchbox, ResourceCache.Strings.top_search, ref TimesFilter, DubGUI.MintSearch);
            row.x = searchbox.xMax + 5;
            row.width = 130f;
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            Widgets.FillableBar(row, Mathf.Clamp01(Mathf.InverseLerp(H_RootUpdate.LastMinGC, H_RootUpdate.LastMaxGC, H_RootUpdate.totalBytesOfMemoryUsed)), DubResources.darkgrey);
            Widgets.Label(row, H_RootUpdate.GarbageCollectionInfo);
            Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(row, ResourceCache.Strings.top_gc_tip);

            row.x = row.xMax + 5;
            row.width = 50f;
            Widgets.Label(row, H_RootUpdate._fpsText);
            TooltipHandler.TipRegion(row, ResourceCache.Strings.top_fps_tip);
            row.x = row.xMax + 5;
            row.width = 90f;
            Widgets.Label(row, H_RootUpdate.tps);
            TooltipHandler.TipRegion(row, ResourceCache.Strings.top_tps_tip);
            row.x = row.xMax + 5;
            row.width = 30f;
            Text.Font = GameFont.Medium;
        }
    }
}
