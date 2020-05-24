using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse.AI.Group;

namespace DubsAnalyzer
{
    [ProfileMode("ColonistBarOnGUI", UpdateMode.GUI)]
    [HarmonyPatch(typeof(ColonistBar), nameof(ColonistBar.ColonistBarOnGUI))]
    internal class H_ColonistBarOnGUI
    {
        public static bool Active = false;
        public static bool Prefix(ref string __state)
        {
            if (Active)
            {
                __state = "ColonistBarOnGUI";
                Analyzer.Start(__state);
            }
            return true;
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