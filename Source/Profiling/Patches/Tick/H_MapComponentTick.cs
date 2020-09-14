using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI.Group;

namespace Analyzer.Profiling
{
    [Entry("MapComponentTick", Category.Tick)]
    internal class H_MapComponentTick
    {
        public static bool Active = false;
        private static Dictionary<string, MethodInfo> methDict = new Dictionary<string, MethodInfo>();

        [HarmonyPriority(Priority.Last)]
        public static void Start(object __instance, MethodBase __originalMethod, ref Profiler __state)
        {
            if (!Active) return;
            string state = string.Empty;
            if (__instance != null)
            {
                state = $"{__instance.GetType().Name}.{__originalMethod.Name}";
            }
            else
            if (__originalMethod.ReflectedType != null)
            {
                state = $"{__originalMethod.ReflectedType.Name}.{__originalMethod.Name}";
            }
            __state = ProfileController.Start(state, null, null, null, null, __originalMethod);
        }

        [HarmonyPriority(Priority.First)]
        public static void Stop(Profiler __state)
        {
            if (Active)
            {
                __state.Stop();
            }
        }

        public static void ProfilePatch()
        {
            HarmonyMethod P = new HarmonyMethod(typeof(H_MapComponentTick), nameof(Prefix));
            MethodInfo D = AccessTools.Method(typeof(MapComponentUtility), nameof(MapComponentUtility.MapComponentTick));
            Modbase.Harmony.Patch(D, P);


            HarmonyMethod go = new HarmonyMethod(typeof(H_MapComponentTick), nameof(Start));
            HarmonyMethod biff = new HarmonyMethod(typeof(H_MapComponentTick), nameof(Stop));

            void slop(Type e, string s)
            {
                Modbase.Harmony.Patch(AccessTools.Method(e, s), go, biff);
            }

            slop(typeof(WildAnimalSpawner), nameof(WildAnimalSpawner.WildAnimalSpawnerTick));
            slop(typeof(WildPlantSpawner), nameof(WildPlantSpawner.WildPlantSpawnerTick));
            slop(typeof(PowerNetManager), nameof(PowerNetManager.PowerNetsTick));
            slop(typeof(SteadyEnvironmentEffects), nameof(SteadyEnvironmentEffects.SteadyEnvironmentEffectsTick));
            slop(typeof(LordManager), nameof(LordManager.LordManagerTick));
            slop(typeof(PassingShipManager), nameof(PassingShipManager.PassingShipManagerTick));
            slop(typeof(VoluntarilyJoinableLordsStarter), nameof(VoluntarilyJoinableLordsStarter.VoluntarilyJoinableLordsStarterTick));
            slop(typeof(GameConditionManager), nameof(GameConditionManager.GameConditionManagerTick));
            slop(typeof(WeatherManager), nameof(WeatherManager.WeatherManagerTick));
            slop(typeof(ResourceCounter), nameof(ResourceCounter.ResourceCounterTick));
            slop(typeof(WeatherDecider), nameof(WeatherDecider.WeatherDeciderTick));
            slop(typeof(FireWatcher), nameof(FireWatcher.FireWatcherTick));
        }

        private static bool Prefix(MethodBase __originalMethod, Map map)
        {
            if (!Active) return true;

            var components = map.components;
            for (int i = 0; i < components.Count; i++)
            {

                MapComponent comp = components[i];
                var name = comp.GetType().FullName;

                if (!methDict.TryGetValue(name, out var meth))
                {
                    meth = comp.GetType().GetMethod("MapComponentTick", BindingFlags.Public | BindingFlags.Instance);
                    methDict[name] = meth;
                }

                var prof = ProfileController.Start(name, () => $"{name}", null, null, null, meth);
                comp.MapComponentTick();
                prof.Stop();
            }

            return false;
        }
    }
}