using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Analyzer.Profiling
{
    [Entry("entry.gui.windowstack", Category.GUI)]
    internal class H_WindowStackOnGUI
    {
        public static bool Active = false;

        public static void ProfilePatch()
        {
            original = AccessTools.Method(typeof(WindowStack), nameof(WindowStack.WindowStackOnGUI));
            Modbase.Harmony.Patch(original, transpiler: new HarmonyMethod(typeof(H_WindowStackOnGUI), nameof(Transpiler)));
        }

        private static MethodInfo original;

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            instructions = CallSiteProfiling.WrapCalls(instructions, __originalMethod,
                AccessTools.Method(typeof(Window), nameof(Window.ExtraOnGUI)), AccessTools.Method(typeof(H_WindowStackOnGUI), nameof(ExtraOnGUI)));
            return CallSiteProfiling.WrapCalls(instructions, __originalMethod,
                AccessTools.Method(typeof(Window), nameof(Window.WindowOnGUI)), AccessTools.Method(typeof(H_WindowStackOnGUI), nameof(WindowOnGUI)));
        }

        public static void ExtraOnGUI(Window window)
        {
            if (!Active)
            {
                window.ExtraOnGUI();
                return;
            }

            var prof = ProfileController.Start(Key(window, "ExtraOnGUI"), null, null, original);
            window.ExtraOnGUI();
            prof?.Stop();
        }

        public static void WindowOnGUI(Window window)
        {
            if (!Active)
            {
                window.WindowOnGUI();
                return;
            }

            var prof = ProfileController.Start(Key(window, "WindowOnGUI"), null, null, original);
            window.WindowOnGUI();
            prof?.Stop();
        }

        private static string Key(Window window, string call)
            => window is ImmediateWindow
                ? string.Intern($"{window.GetType()} {call} {window.ID}")
                : string.Intern($"{window.GetType()} {call}");
    }
}