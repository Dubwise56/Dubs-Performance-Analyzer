using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    public static class Panel_Tabs
    {
        public static float width = 220f;
        private static Vector2 ScrollPosition = Vector2.zero;
        public static Listing_Standard listing = new Listing_Standard();
        public static float yOffset;
        private static float ListHeight;

        public static void Draw(Rect rect, IEnumerable<Tab> tabs)
        {
            var ListerBox = rect.LeftPartPixels(width);
            ListerBox.width -= 10f;
            Widgets.DrawMenuSection(ListerBox);
            ListerBox = ListerBox.ContractedBy(4f);

            var baseRect = ListerBox.AtZero();
            baseRect.width -= 16f;
            baseRect.height = ListHeight;

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Tiny;

            yOffset = 0f;

            Widgets.BeginScrollView(ListerBox, ref ScrollPosition, baseRect);
            GUI.BeginGroup(baseRect);
            listing.Begin(baseRect);

            foreach (var tab in tabs)
            {
                if (tab.category == Category.Settings || tab.entries.Count > 0)
                {
                    DrawTabs(tab);
                }
            }

            listing.End();
            GUI.EndGroup();
            Widgets.EndScrollView();

            DubGUI.ResetFont();
            ListHeight = yOffset;
        }

        private static void DrawTabs(Tab tab)
        {
            DubGUI.ResetFont();
            yOffset += 40f;

            var row = listing.GetRect(30f);
            if (tab.category == Category.Settings)
            {
                if (GUIController.GetCurrentTab == tab)
                {
                    Widgets.DrawHighlightSelected(row);
                }
                Widgets.DrawHighlightIfMouseover(row);
                if (Widgets.ButtonInvisible(row)) tab.onClick();
            }
            else
            {
                Widgets.DrawHighlightIfMouseover(row);
                Widgets.DrawTextureFitted(row.RightPartPixels(row.height), tab.collapsed ? DubGUI.DropDown : DubGUI.FoldUp, 1f);
                if (Widgets.ButtonInvisible(row))
                {
                    tab.collapsed = !tab.collapsed;
                }
            }

            row.x += 5f;
            Widgets.Label(row, tab.Label);

            TooltipHandler.TipRegion(row, tab.Tip);

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Tiny;

            if (tab.collapsed) return;

            foreach (var entry in tab.entries)
            {
                DrawEntry(ref row, entry);
            }
        }

        private static void DrawEntry(ref Rect row, KeyValuePair<Entry, Type> entry)
        {
            row = listing.GetRect(30f);
            Widgets.DrawHighlightIfMouseover(row);

            if (GUIController.CurrentEntry == entry.Key)
                Widgets.DrawOptionSelected(row);

            row.x += 20f;
            yOffset += 30f;

            Widgets.Label(row, entry.Key.name);

            if (Widgets.ButtonInvisible(row))
            {
                GUIController.SwapToEntry(entry.Key.name);
            }

            if (entry.Key.isClosable)
            {
                if (Input.GetMouseButtonDown(1) && row.Contains(Event.current.mousePosition))
                {
                    var options = new List<FloatMenuOption>
                    {
                        new FloatMenuOption("Close", () => GUIController.RemoveEntry(entry.Key.name))
                    };
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }

            TooltipHandler.TipRegion(row, entry.Key.tip);

            if (GUIController.CurrentEntry == entry.Key)
            {
                var firstEntry = true;
                foreach (var keySetting in entry.Key.Settings)
                {
                    if (keySetting.Key.FieldType == typeof(bool))
                    {
                        row = listing.GetRect(30f);
                        row.x += 20f;
                        GUI.color = Widgets.OptionSelectedBGBorderColor;
                        Widgets.DrawLineVertical(row.x, row.y, 15f);

                        if (!firstEntry)
                        {
                            Widgets.DrawLineVertical(row.x, row.y - 15f, 15f);
                        }

                        row.x += 10f;
                        Widgets.DrawLineHorizontal(row.x - 10f, row.y + 15f, 10f);
                        GUI.color = Color.white;
                        yOffset += 30f;

                        var cur = (bool)keySetting.Key.GetValue(null);

                        if (DubGUI.Checkbox(row, keySetting.Value.name, ref cur))
                        {
                            keySetting.Key.SetValue(null, cur);

                            GUIController.ResetProfilers();
                        }
                    }

                    if (keySetting.Value.tip != null)
                    {
                        TooltipHandler.TipRegion(row, keySetting.Value.tip);
                    }

                    firstEntry = false;
                }
            }
        }
    }
}