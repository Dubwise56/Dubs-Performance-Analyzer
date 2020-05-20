using HarmonyLib;
using RimWorld;

namespace DubsAnalyzer
{
     [ProfileMode("ResourceReadoutOnGUI", UpdateMode.GUI)]
    [HarmonyPatch(typeof(ResourceReadout), nameof(ResourceReadout.ResourceReadoutOnGUI))]
    internal class H_ResourceReadoutOnGUI
    {
        public static bool Active=false;
        public static bool Prefix()
        {
            if (Active)
            {
                Analyzer.Start("ResourceReadoutOnGUI");//, () => "ResourceReadout.ResourceReadoutOnGUI");
            }
            return true;
        }

        public static void Postfix()
        {
            if (Active)
            {
                Analyzer.Stop("ResourceReadoutOnGUI");
            }
        }
    }
}