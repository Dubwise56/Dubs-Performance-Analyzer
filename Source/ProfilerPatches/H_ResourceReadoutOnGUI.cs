using HarmonyLib;
using RimWorld;
using System.Reflection;

namespace DubsAnalyzer
{
     [ProfileMode("ResourceReadoutOnGUI", UpdateMode.GUI)]
    [HarmonyPatch(typeof(ResourceReadout), nameof(ResourceReadout.ResourceReadoutOnGUI))]
    internal class H_ResourceReadoutOnGUI
    {
        public static bool Active=false;
        public static bool Prefix(MethodBase __originalMethod)
        {
            if (Active)
            {
                Analyzer.Start("ResourceReadoutOnGUI", null, null, null, null, __originalMethod as MethodInfo);//, () => "ResourceReadout.ResourceReadoutOnGUI");
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