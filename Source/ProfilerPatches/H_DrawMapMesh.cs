using HarmonyLib;
using DubsAnalyzer;
using Verse;

namespace DubsAnalyzer
{
    [ProfileMode("DrawMapMesh", UpdateMode.Update)]
    [HarmonyPatch(typeof(MapDrawer), nameof(MapDrawer.DrawMapMesh))]
    internal class H_DrawMapMesh
    {
        public static bool Active=false;

        public static void Prefix(ref string __state)
        {
            if (!Active)
            {
                return;
            }
            __state = nameof(MapDrawer.DrawMapMesh);
            Analyzer.Start(__state);
        }

        public static void Postfix(string __state)
        {
            if (Active)
            {
                Analyzer.Stop(__state);
            }
        }
    }
}