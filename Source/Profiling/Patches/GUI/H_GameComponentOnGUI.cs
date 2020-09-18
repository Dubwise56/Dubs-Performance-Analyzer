using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace Analyzer.Profiling
{
    [Entry("Game Component", Category.GUI)]
    public static class H_GameComponentUpdateGUI
    {
        public static bool Active = false;

        public static IEnumerable<MethodInfo> GetPatchMethods() => typeof(GameComponent).AllSubclasses().Select(gc => gc.GetMethod("GameComponentOnGUI"));
        public static string GetLabel(GameComponent __instance) => __instance.GetType().Name;
    }
}
