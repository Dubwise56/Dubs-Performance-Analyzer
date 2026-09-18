using System;
using System.Collections;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    [Entry("entry.gui.dotabs", Category.GUI)]
    internal class H_DoTabs
    {
        public static bool Active = false;

        public static string GetLabel() => "InspectPaneUtility - DoTabs";

        public static void ProfilePatch()
        {
            // The compiler-generated local function name carries a number that changes between game versions (17_0 in 1.5, 18_0 in 1.6)
            Modbase.Harmony.Patch(AccessTools.FirstMethod(typeof(InspectPaneUtility), m => m.Name.StartsWith("<DoTabs>g__Do")), new HarmonyMethod(typeof(H_DoTabs), "Prefix"), new HarmonyMethod(typeof(H_DoTabs), "Postfix"));

        }

        public static bool Prefix(MethodBase __originalMethod, InspectTabBase tab, ref string __state)
        {
            if (!Active) return true;

            __state = string.Empty;
            if (tab is InspectTabBase f)
            {
                __state = string.Intern($"{tab.GetType()} {tab.labelKey}");
            }
            else
            {
                __state = string.Intern($"{tab.GetType()}");
            }

            ProfileController.Start(__state, null, tab.GetType(), __originalMethod);

            return true;
        }

        public static void Postfix(string __state)
        {
            if (Active)
            {
                ProfileController.Stop(__state);
            }
        }
    }
}