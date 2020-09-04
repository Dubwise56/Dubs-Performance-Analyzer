using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Analyzer
{
    [Entry("WindowStackOnGUI", UpdateMode.Update, "WindowPatchTipKey")]
    [HarmonyPatch(typeof(WindowStack), nameof(WindowStack.WindowStackOnGUI))]
    internal class H_WindowStackOnGUI
    {
        public static bool Active = false;

        public static bool Prefix(MethodBase __originalMethod, WindowStack __instance)
        {
            if (!Active) return true;

            __instance.windowStackOnGUITmpList.Clear();
            __instance.windowStackOnGUITmpList.AddRange(__instance.windows);
            for (int i = __instance.windowStackOnGUITmpList.Count - 1; i >= 0; i--)
            {
                string name = string.Empty;
                if (__instance.windowStackOnGUITmpList[i] is ImmediateWindow)
                {
                    name = string.Intern(
                        $"{__instance.windowStackOnGUITmpList[i].GetType()} ExtraOnGUI {__instance.windowStackOnGUITmpList[i].ID}");
                }
                else
                {
                    name = string.Intern($"{__instance.windowStackOnGUITmpList[i].GetType()} ExtraOnGUI");
                }

                Profiler prof = Modbase.Start(name, null, null, null, null, __originalMethod);
                __instance.windowStackOnGUITmpList[i].ExtraOnGUI();
                prof.Stop();
            }

            __instance.UpdateImmediateWindowsList();
            __instance.windowStackOnGUITmpList.Clear();
            __instance.windowStackOnGUITmpList.AddRange(__instance.windows);
            for (int j = 0; j < __instance.windowStackOnGUITmpList.Count; j++)
            {
                if (__instance.windowStackOnGUITmpList[j].drawShadow)
                {
                    GUI.color = new Color(1f, 1f, 1f, __instance.windowStackOnGUITmpList[j].shadowAlpha);
                    Widgets.DrawShadowAround(__instance.windowStackOnGUITmpList[j].windowRect);
                    GUI.color = Color.white;
                }

                string name = string.Empty;
                if (__instance.windowStackOnGUITmpList[j] is ImmediateWindow)
                {
                    name = string.Intern(
                        $"{__instance.windowStackOnGUITmpList[j].GetType()} WindowOnGUI {__instance.windowStackOnGUITmpList[j].ID}");
                }
                else
                {
                    name = string.Intern($"{__instance.windowStackOnGUITmpList[j].GetType()} WindowOnGUI");
                }

                Profiler prof = Modbase.Start(name, null, null, null, null, __originalMethod);
                __instance.windowStackOnGUITmpList[j].WindowOnGUI();
                prof.Stop();
            }

            if (__instance.updateInternalWindowsOrderLater)
            {
                __instance.updateInternalWindowsOrderLater = false;
                __instance.UpdateInternalWindowsOrder();
            }

            return false;
        }
    }
}