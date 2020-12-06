using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.Profiling;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace Analyzer.Fixes
{
    public static class FixPatches
    {
        private static List<Fix> fixes = null;
        public static List<Fix> Fixes
        {
            get
            {
                if (fixes == null)
                {
                    InitialiseFixes();
                }
                return fixes;
            }
        }

        public static void InitialiseFixes()
        {
            var modes = typeof(Fix).AllSubclasses().ToList();
            fixes = new List<Fix>(modes.Count());

            foreach (var mode in modes)
            {
                var patch = (Fix) Activator.CreateInstance(mode, null);
                patch.Initialise(mode);
                fixes.Add(patch);
            }
        }

        public static void OnGameInit(Game game)
        {
            foreach (var fix in Fixes.Where(f => f.EnabledRefAccess()))
                fix.OnGameInit(game, Modbase.StaticHarmony);
        }

        public static void OnGameLoad(Game game)
        {
            foreach (var fix in Fixes.Where(f => f.EnabledRefAccess()))
                fix.OnGameLoaded(game, Modbase.StaticHarmony);
        }

        public static void Draw(Listing_Standard standard)
        {
            //DubGUI.CenterText( () => standard.Label("Fixes"));

            var r = standard.GetRect(Text.LineHeight);
            r.x += 30;
            r.width -= 30;
            Widgets.Label(r, "Fixes");

            foreach (var fix in Fixes)
            {
                fix.Draw(standard);
            }
        }

        public static void ExposeData()
        {

        }
    }
}
