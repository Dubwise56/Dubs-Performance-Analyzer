using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DubsAnalyzer
{

    [ProfileMode("World Tick", UpdateMode.Tick)]
    public static class H_WorldPawns 
    {
        public static bool Active = false;
        public static void SetActive(bool t)
        {
            Active = t;
        }

        public static void ProfilePatch()
        {
            var go = new HarmonyMethod(typeof(H_WorldPawns), nameof(Start));
            var biff = new HarmonyMethod(typeof(H_WorldPawns), nameof(Stop));

            void slop(Type e, string s)
            {
                Analyzer.harmony.Patch(AccessTools.Method(e, s), go, biff);
            }
            slop(typeof(WorldPawns), nameof(WorldPawns.WorldPawnsTick));
            slop(typeof(FactionManager), nameof(FactionManager.FactionManagerTick));
            slop(typeof(WorldObjectsHolder), nameof(WorldObjectsHolder.WorldObjectsHolderTick));
            slop(typeof(WorldPathGrid), nameof(WorldPathGrid.WorldPathGridTick));

            Analyzer.harmony.Patch(AccessTools.Method(typeof(WorldComponentUtility), nameof(WorldComponentUtility.WorldComponentTick)), new HarmonyMethod(typeof(H_WorldPawns), nameof(WorldComponentTick)));
        }

        public static bool WorldComponentTick(MethodBase __originalMethod, World world)
        {
            if (!Active) return true;

            var components = world.components;
            for (var i = 0; i < components.Count; i++)
            {
                try
                {
                    var picard = components[i].GetType().Name;
                    Analyzer.Start(picard, null, null, null, null, __originalMethod as MethodInfo);
                    components[i].WorldComponentTick();
                    Analyzer.Stop(picard);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            return false;
        }

        [HarmonyPriority(Priority.Last)]
        public static void Start(object __instance, MethodBase __originalMethod, ref string __state)
        {
            if (!Active) return;

            if (__instance != null)
            {
                __state = __instance.GetType().Name;
            }
            else if (__originalMethod.ReflectedType != null)
            {
                __state = __originalMethod.ReflectedType.Name;
            }
            else
            {
                __state = __originalMethod.GetType().Name;
            }

            Analyzer.Start(__state, null, null, null, null, __originalMethod as MethodInfo);
        }

        [HarmonyPriority(Priority.First)]
        public static void Stop(string __state)
        {
            if (Active && !string.IsNullOrEmpty(__state))
            {
                Analyzer.Stop(__state);
            }
        }
    }
}