using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace Analyzer.Profiling
{
    [Entry("entry.update.gamecomponent", Category.Update, "entry.update.gamecomponent.tooltip")]
    public static class H_GameComponentUpdate
    {
        public static bool Active = false;

        public static IEnumerable<MethodInfo> GetPatchMethods() => typeof(GameComponent).AllSubclasses().Select(gc => gc.GetMethod("GameComponentUpdate"));
        public static string GetLabel(GameComponent __instance) => __instance.GetType().Name;
    }

}