using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    public class Panel_Patches : IBottomTabRow
    {
        private float x = 0;
        private float y = 0;
        private Vector2 scroll = Vector2.zero;

        public void Draw(Rect inrect, GeneralInformation? currentInformation)
        {
            if (currentInformation == null) return;
            inrect = inrect.ContractedBy(4);

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;

            x = 0;

            var viewrect = inrect;
            viewrect.x += 10;
            viewrect.width -= 28;
            viewrect.height = y;

            var row = viewrect;
            row.height = 40;
            row.width = Mathf.Max(x, viewrect.width);

            Widgets.BeginScrollView(inrect, ref scroll, viewrect);

            foreach (var patch in currentInformation?.patches)
            {
                var meth = $"{patch.typeName} : {patch.methodName}";
                row.width = meth.GetWidthCached();

                if (row.width > x)
                {
                    x = row.width;
                }

                Widgets.Label(row, meth);
                
                Widgets.DrawHighlightIfMouseover(row);

                if (Mouse.IsOver(row))
                {
                    TooltipHandler.TipRegion(row, $"{Strings.panel_mod_name}: {patch.modName}\n{Strings.panel_patch_type}: {patch.patchType}");
                }

                if (Input.GetMouseButtonDown(1) && row.Contains(Event.current.mousePosition)) // mouse button right
                {
                    var options = new List<FloatMenuOption>()
                    {
                        new FloatMenuOption(Strings.panel_opengithub, () => Panel_BottomRow.OpenGithub($"{patch.typeName}.{patch.methodName}")),
                        new FloatMenuOption(Strings.panel_opendnspy, () => Panel_BottomRow.OpenDnspy(patch.method))
                    };

                    Find.WindowStack.Add(new FloatMenu(options));
                }

                row.y = row.yMax;

                y = row.yMax;
            }

            Widgets.EndScrollView();

            DubGUI.ResetFont();
        }

        public void ResetState(GeneralInformation? _)
        {
            x = 0;
            y = 0;
            scroll = Vector2.zero;
        }
    }
}