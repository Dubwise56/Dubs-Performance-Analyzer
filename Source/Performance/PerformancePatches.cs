﻿using System;
using System.Collections.Generic;
using System.Linq;
 
using Analyzer.Profiling;
using Verse;

namespace Analyzer.Performance
{
    public static class PerformancePatches
    {
        private static List<PerfPatch> perfPatches;
        public static Dictionary<string, Action> onDisabled = new Dictionary<string, Action>();

        private static List<bool> allEnabled;

        public static List<bool> AllEnabled
        {
            get
            {
                if (allEnabled.NullOrEmpty())
                {
                    allEnabled = new List<bool>();
                    for (var i = 0; i < Enum.GetNames(typeof(PerformanceCategory)).Length; i++)
                    {
                        allEnabled.Add(false);
                    }
                }

                return allEnabled;
            }
        }

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
            var modes = typeof(PerfPatch).AllSubclasses().ToList();
            perfPatches = new List<PerfPatch>();

            foreach (var mode in modes)
            {
                var patch = (PerfPatch) Activator.CreateInstance(mode, null);
                patch.Initialise(mode);
                perfPatches.Add(patch);
            }
        }

        public static void Draw(Listing_Standard listing)
        {
            DrawCategory(listing, PerformanceCategory.Optimizes, "settings.Optimizations".Tr());
            listing.GapLine();
            DrawCategory(listing, PerformanceCategory.Overrides, "settings.Overrides".Tr());
            listing.GapLine();
            DrawCategory(listing, PerformanceCategory.Removes, "settings.Disabling".Tr());
            listing.GapLine();
        }

        private static void DrawCategory(Listing_Standard standard, PerformanceCategory category, string stringifiedCat)
        {
            var stateChange = false;
            var enableAll = AllEnabled[(int) category];

            if (category == PerformanceCategory.Removes)
            {
                var r = standard.GetRect(Text.LineHeight);
                r.x += 30;
                r.width -= 30;
                Widgets.Label(r, stringifiedCat);
            }
            else
            {
                var rect = standard.GetRect(Text.LineHeight);
                if (DubGUI.Checkbox(rect, stringifiedCat, ref enableAll))
                {
                    stateChange = true;
                    AllEnabled[(int)category] = enableAll;
                }
            }

            standard.Gap();

            foreach (var p in Patches.Where(p => p.Category == category))
            {
                if (stateChange)
                {
                    p.EnabledRefAccess() = enableAll;
                    p.CheckState();
                }

                p.Draw(standard);
            }
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

            foreach (var patch in Patches)
            {
                patch.ExposeData();
            }
        }
    }
}