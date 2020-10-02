using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Analyzer.Profiling;
using RimWorld;
using UnityEngine;
using Verse;

namespace Analyzer.Performance
{
    public static class PerformancePatches
    {
        private static List<PerfPatch> perfPatches = null;
        public static Dictionary<string, Action> onDisabled = new Dictionary<string, Action>();
        public static List<bool> allEnabled;

        public static List<PerfPatch> Patches
        {
            get
            {
                if (perfPatches == null) InitialisePatches();
                return perfPatches;
            }
        }

        public static void InitialisePatches()
        {
            var modes = typeof(PerfPatch).AllSubclasses()
                .ToList();
            perfPatches = new List<PerfPatch>(modes.Count());

            foreach (var mode in modes)
            {
                var patch = (PerfPatch) Activator.CreateInstance(mode, null);
                patch.Initialise(mode);
                perfPatches.Add(patch);
            }
        }

        public static void Draw(ref Listing_Standard listing)
        {
            var standard = new Listing_Standard();
            var rect = new Rect(0, listing.curY, listing.ColumnWidth, 99999);
            GUI.BeginGroup(rect);
            rect = rect.AtZero();
            standard.Begin(rect);
            standard.ColumnWidth = (standard.ColumnWidth - 18) / 3;

            Widgets.DrawLineVertical(standard.curX + (standard.ColumnWidth + 9) * 3, standard.curY, 999f);

            DrawCategory(ref standard, PerformanceCategory.Optimizes);
            DrawCategory(ref standard, PerformanceCategory.Overrides);
            DrawCategory(ref standard, PerformanceCategory.Removes);

            // make sure the horizontal line looks exactly like gapline, and covers the entire table
            var color = GUI.color;
            GUI.color = color * new Color(1f, 1f, 1f, 0.4f);
            Widgets.DrawLineHorizontal(listing.curX, standard.curY + 33, (standard.ColumnWidth + 34) * 3);
            GUI.color = color;

            standard.End();
            GUI.EndGroup();
        }

        private static void DrawCategory(ref Listing_Standard standard, PerformanceCategory category)
        {
            Widgets.DrawLineVertical(standard.curX + standard.columnWidthInt, standard.curY, 999f);
            var stateChange = false;
            var enableAll = allEnabled[(int) category];
            var stringifiedCat = category.ToString();
                
            // FooABar -> Foo A Bar
            for (int i = 0; i < stringifiedCat.Length; i++)
            {
                var c = stringifiedCat[i];
                if (!char.IsUpper(c) || i == 0) continue;

                stringifiedCat = stringifiedCat.Insert(i, " ");
                i++;
            }

            if (category == PerformanceCategory.Removes)
            {
                DubGUI.Heading(standard, stringifiedCat);
            }
            else
            {
                if (DubGUI.HeadingCheckBox(standard, stringifiedCat, ref enableAll))
                {
                    stateChange = true;
                    allEnabled[(int) category] = enableAll;
                }
            }

            standard.curY += 6; // emulate the gapline we draw 

            var patches = Patches.Where(p => p.Category == category);
            foreach (var p in patches)
            {
                if (stateChange)
                {
                    p.EnabledRefAccess() = enableAll;
                    p.CheckState();
                }

                p.Draw(standard);
            }

            standard.NewColumn();
        }

        public static void ActivateEnabledPatches()
        {
            foreach (var patch in Patches)
            {
                if (!patch.EnabledRefAccess() || patch.isPatched) continue;

                patch.OnEnabled(Modbase.StaticHarmony);
                patch.isPatched = true;
            }
        }

        public static void ClosePatches()
        {
            foreach (var disabled in onDisabled.Values)
                disabled();

            onDisabled.Clear();
        }

        public static void ExposeData()
        {
            Scribe_Collections.Look(ref allEnabled, "allEnabled", LookMode.Value);
            if (allEnabled.NullOrEmpty())
            {
                allEnabled = new List<bool>();
                for (int i = 0; i < Enum.GetNames(typeof(PerformanceCategory)).Length; i++)
                {
                    allEnabled.Add(false);
                }
            }

            foreach (var patch in Patches)
            {
                patch.ExposeData();
            }
        }
    }
}