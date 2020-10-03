using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace Analyzer.Profiling
{
    [Entry("entry.tick.gamecomponent", Category.Tick, "entry.tick.gamecomponent.tooltip")]
    public static class H_GameComponent
    {
        public static bool Active = false;

        public static IEnumerable<MethodInfo> GetPatchMethods()
        {
            var passedTypes = new List<Type>();
            var types = typeof(GameComponent).AllSubclassesNonAbstract();

            foreach (var type in types)
            {
                if(!passedTypes.Any(ty => type.IsAssignableFrom(ty)))
                    passedTypes.Add(type);
            }


            return passedTypes.Select(gc => gc.GetMethod("GameComponentTick"));
        }
        public static string GetLabel(GameComponent __instance) => __instance.GetType().Name;
    }
}

