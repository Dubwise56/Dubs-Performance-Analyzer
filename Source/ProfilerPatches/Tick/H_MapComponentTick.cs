using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace DubsAnalyzer
{
    [ProfileMode("MapComponentTick", UpdateMode.Tick)]
    internal class H_MapComponentTick
    {
        public static bool Active = false;

        [HarmonyPriority(Priority.Last)]
        public static void Start(object __instance, MethodBase __originalMethod, ref Profiler __state)
        {
            if (!Active || !AnalyzerState.CurrentlyRunning) return;
            var state = string.Empty;
            if (__instance != null)
            {
                state = $"{__instance.GetType().Name}.{__originalMethod.Name}";
            }
            else
            if (__originalMethod.ReflectedType != null)
            {
                state = $"{__originalMethod.ReflectedType.Name}.{__originalMethod.Name}";
            }
            __state = Analyzer.Start(state, null, null, null, null, __originalMethod);
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
            var P = new HarmonyMethod(typeof(H_MapComponentTick), nameof(Prefix));
            var D = AccessTools.Method(typeof(MapComponentUtility), nameof(MapComponentUtility.MapComponentTick));
            Analyzer.harmony.Patch(D, P);


            var go = new HarmonyMethod(typeof(H_MapComponentTick), nameof(Start));
            var biff = new HarmonyMethod(typeof(H_MapComponentTick), nameof(Stop));

            void slop(Type e, string s)
            {
                Analyzer.harmony.Patch(AccessTools.Method(e, s), go, biff);
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
            if (!Active)
            {
                return true;
            }

            var components = map.components;
            var c = components.Count;
            for (var i = 0; i < c; i++)
            {
                try
                {
                    var comp = components[i];

                    var prof = Analyzer.Start(comp.GetType().FullName, () => $"{comp.GetType()}", null, null, null, __originalMethod);
                    comp.MapComponentTick();
                    prof.Stop();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }

            return false;
        }
    }
}