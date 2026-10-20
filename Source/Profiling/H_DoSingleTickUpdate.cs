using System.Linq;
using HarmonyLib;
using Verse;

namespace Analyzer.Profiling
{
    internal class H_DoSingleTickUpdate
    {
        private static int outerPhase;

        public static void Prefix()
        {
            outerPhase = ProfileController.phase;
            ProfileController.phase = ProfileController.PhaseTick;

            if (GUIController.CurrentCategory == Category.Tick) // If we in Tick mode, start our update (can happen multiple times p frame)
                ProfileController.BeginUpdate();
        }

        public static void Postfix()
        {
            if (GUIController.CurrentCategory == Category.Tick) // If we in Tick mode, finish our update (can happen multiple times p frame)
                ProfileController.EndUpdate();

            ProfileController.phase = outerPhase;
        }
    }
}