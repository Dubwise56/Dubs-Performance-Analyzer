using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Reflection;
using UnityEngine;
using Verse;

namespace DubsAnalyzer
{


    [ProfileMode("ColonistBarOnGUI", UpdateMode.GUI)]
    [HarmonyPatch(typeof(ColonistBar), nameof(ColonistBar.ColonistBarOnGUI))]
    internal class H_ColonistBarOnGUI
    {
        public static bool Active = false;
        public static bool Prefix(MethodBase __originalMethod, ref string __state)
        {
            if (Active)
            {
                __state = "ColonistBarOnGUI";
                Analyzer.Start(__state, null, null, null, null, __originalMethod as MethodInfo);
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