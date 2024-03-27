using System.Linq;
using HarmonyLib;
using Verse;

namespace Analyzer.Profiling
{
    internal class H_DoSingleTickUpdate
    {
        public static void Prefix()
        {
            if (H_RootUpdate.Active)
            {
                ProfileController.Start("Tick");
            }

            if (GUIController.CurrentCategory == Category.Tick) // If we in Tick mode, start our update (can happen multiple times p frame)
                ProfileController.BeginUpdate();
        }

        public static void Postfix()
        {
            if (GUIController.CurrentCategory == Category.Tick) // If we in Tick mode, finish our update (can happen multiple times p frame)
                ProfileController.EndUpdate();
            
            if (H_RootUpdate.Active)
            {
                ProfileController.Stop("Tick");
            }
        }
    }
}