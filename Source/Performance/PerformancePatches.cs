using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Analyzer.Performance
{
    public static class PerformancePatches
    {
        private static List<PerfPatch> perfPatches = null;
        public static List<PerfPatch> Patches
        {
            get
            {
                if (perfPatches == null)
                {
                    InitialisePatches();
                }
                return perfPatches;
            }
        }

        public static Dictionary<string, Action> ondisabled = new Dictionary<string, Action>();

        public static void InitialisePatches()
        {
            var modes = typeof(PerfPatch).AllSubclasses();
            perfPatches = new List<PerfPatch>(modes.Count());

            foreach (var mode in modes)
            {
                var patch = (PerfPatch)Activator.CreateInstance(mode, null);
                patch.Initialise(mode);
                perfPatches.Add(patch);
            }
        }

        public static void Draw(ref Listing_Standard standard)
        {
            foreach(var patch in Patches)
            {
                patch.Draw(standard);
            }
        }

        public static void ClosePatches()
        {
            foreach(var disabled in ondisabled.Values)
                disabled();
        }

        public static void ExposeData()
        {
            foreach (var patch in Patches)
            {
                patch.ExposeData();
                if(patch.EnabledRefAccess() && !patch.isPatched) // Our patch should currently be active
                {
                    patch.OnEnabled(Modbase.StaticHarmony);
                    patch.isPatched = true;
                }
            }
        }
    }
}
