using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;

namespace DubsAnalyzer
{
    [ProfileMode("UIRootOnGUI", UpdateMode.GUI)]
    [HarmonyPatch(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootOnGUI))]
    internal class H_UIRootOnGUI
    {
        public static bool Active = false;

        public static void ProfilePatch()
        {
            var go = new HarmonyMethod(typeof(H_UIRootOnGUI), nameof(Prefix));
            var biff = new HarmonyMethod(typeof(H_UIRootOnGUI), nameof(Postfix));

            void DoMe(Type t, string m)
            {
               Analyzer.harmony.Patch(AccessTools.Method(t, m), go, biff);
            }

            DoMe(typeof(UnityGUIBugsFixer), nameof(UnityGUIBugsFixer.OnGUI));
            DoMe(typeof(MapInterface), nameof(MapInterface.MapInterfaceOnGUI_BeforeMainTabs));
            DoMe(typeof(MapInterface), nameof(MapInterface.MapInterfaceOnGUI_AfterMainTabs));
            DoMe(typeof(GameComponentUtility), nameof(GameComponentUtility.GameComponentOnGUI));
            DoMe(typeof(MainButtonsRoot), nameof(MainButtonsRoot.MainButtonsOnGUI));
            DoMe(typeof(WindowStack), nameof(WindowStack.WindowStackOnGUI)); 
            DoMe(typeof(AlertsReadout), nameof(AlertsReadout.AlertsReadoutOnGUI));
        }
        public static void Prefix(object __instance, MethodBase __originalMethod, ref string __state)
        {
            if (Active || H_RootUpdate.Active)
            {
                if (__instance != null)
                {
                    __state = __instance.GetType().Name;
                }
                else
                if (__originalMethod.ReflectedType != null)
                {
                    __state = __originalMethod.ReflectedType.Name;
                }
                else
                {
                    __state = __originalMethod.GetType().Name;
                }

                Analyzer.Start(__state, null, null, null, null, __originalMethod as MethodInfo);
            }
        }

        public static void Postfix(string __state)
        {
            if (Active || H_RootUpdate.Active)
            {
                Analyzer.Stop(__state);
            }
        }
    }
}