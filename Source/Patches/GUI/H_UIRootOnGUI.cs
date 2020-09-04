using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using Verse;

namespace Analyzer
{
    [Entry("UIRootOnGUI", Category.Update)]
    [HarmonyPatch(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootOnGUI))]
    internal class H_UIRootOnGUI
    {
        public static bool Active = false;

        public static void ProfilePatch()
        {
            HarmonyMethod go = new HarmonyMethod(typeof(H_UIRootOnGUI), nameof(Prefix));
            HarmonyMethod biff = new HarmonyMethod(typeof(H_UIRootOnGUI), nameof(Postfix));

            void DoMe(Type t, string m)
            {
                Modbase.Harmony.Patch(AccessTools.Method(t, m), go, biff);
            }

            DoMe(typeof(UnityGUIBugsFixer), nameof(UnityGUIBugsFixer.OnGUI));
            DoMe(typeof(MapInterface), nameof(MapInterface.MapInterfaceOnGUI_BeforeMainTabs));
            DoMe(typeof(MapInterface), nameof(MapInterface.MapInterfaceOnGUI_AfterMainTabs));
            DoMe(typeof(GameComponentUtility), nameof(GameComponentUtility.GameComponentOnGUI));
            DoMe(typeof(MainButtonsRoot), nameof(MainButtonsRoot.MainButtonsOnGUI));
            DoMe(typeof(WindowStack), nameof(WindowStack.WindowStackOnGUI));
            DoMe(typeof(AlertsReadout), nameof(AlertsReadout.AlertsReadoutOnGUI));
        }

        [HarmonyPriority(Priority.Last)]
        public static void Prefix(object __instance, MethodBase __originalMethod, ref Profiler __state)
        {
            if (Active || H_RootUpdate.Active)
            {
                string state = string.Empty;
                if (__instance != null)
                {
                    state = __instance.GetType().Name;
                }
                else if (__originalMethod.ReflectedType != null)
                {
                    state = __originalMethod.ReflectedType.Name;
                }
                else
                {
                    state = __originalMethod.GetType().Name;
                }

                __state = Analyzer.Start(state, null, null, null, null, __originalMethod);
            }
        }

        [HarmonyPriority(Priority.First)]
        public static void Postfix(Profiler __state)
        {
            if (Active || H_RootUpdate.Active)
            {
                __state?.Stop();
            }
        }
    }
}