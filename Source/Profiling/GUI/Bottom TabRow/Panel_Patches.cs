using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;
namespace Analyzer.Profiling
{
    public struct GeneralInformation
    {
        public MethodBase method;
        public string modName;
        public string assname;
        public string methodName;
        public string typeName;
        public string patchType;
        public List<GeneralInformation> patches;
    }
    public static class Panel_Patches
    {
        private static float x = 0;
        static float y = 0;
        static Vector2 scroller;

        public static void Draw(Rect inrect, GeneralInformation? currentInformation)
        {
            inrect = inrect.ContractedBy(4);
            if (currentInformation == null || currentInformation.Value.patches.NullOrEmpty()) return;

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

            Widgets.BeginScrollView(inrect, ref scroller, viewrect);

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
                    TooltipHandler.TipRegion(row, $"Mod Name: {patch.modName}\nPatch Type: {patch.patchType}");
                }

                if (Input.GetMouseButtonDown(1) && row.Contains(Event.current.mousePosition)) // mouse button right
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>()
                    {
                        new FloatMenuOption("Open In Github", () => Panel_BottomRow.OpenGithub($"{patch.typeName}.{patch.methodName}")),
                        new FloatMenuOption("Open In Dnspy (requires local path)", () => Panel_BottomRow.OpenDnspy(patch.method))
                    };

                    Find.WindowStack.Add(new FloatMenu(options));
                }

                row.y = row.yMax;

                y = row.yMax;
            }

            Widgets.EndScrollView();

            DubGUI.ResetFont();
        }
    }
}