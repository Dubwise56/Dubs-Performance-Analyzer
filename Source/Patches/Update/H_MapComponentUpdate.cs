using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using Verse;
using Verse.AI.Group;

namespace Analyzer
{
    [Entry("MapComponentUpdate", UpdateMode.Update)]
    // [HarmonyPatch(typeof(MapComponentUtility), nameof(MapComponentUtility.MapComponentTick))]
    internal class H_MapComponentUpdate
    {
        public static bool Active = false;

        [HarmonyPriority(Priority.Last)]
        public static void Start(object __instance, MethodBase __originalMethod, ref Profiler __state)
        {
            if (!Active || !AnalyzerState.CurrentlyRunning) return;
            string state = string.Empty;
            if (__instance != null)
            {
                state = $"{__instance.GetType().Name}.{__originalMethod.Name}";
            }
            else if (__originalMethod.ReflectedType != null)
            {
                state = $"{__originalMethod.ReflectedType.Name}.{__originalMethod.Name}";
            }

            __state = Modbase.Start(state, null, null, null, null, __originalMethod);
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
            HarmonyMethod P = new HarmonyMethod(typeof(H_MapComponentUpdate), nameof(Prefix));
            MethodInfo D = AccessTools.Method(typeof(MapComponentUtility), nameof(MapComponentUtility.MapComponentUpdate));
            Modbase.harmony.Patch(D, P);


            HarmonyMethod go = new HarmonyMethod(typeof(H_MapComponentUpdate), nameof(Start));
            HarmonyMethod biff = new HarmonyMethod(typeof(H_MapComponentUpdate), nameof(Stop));

            void slop(Type e, string s)
            {
                Modbase.harmony.Patch(AccessTools.Method(e, s), go, biff);
            }

            slop(typeof(SkyManager), nameof(SkyManager.SkyManagerUpdate));
            slop(typeof(PowerNetManager), nameof(PowerNetManager.UpdatePowerNetsAndConnections_First));
            slop(typeof(RegionGrid), nameof(RegionGrid.UpdateClean));
            slop(typeof(RegionAndRoomUpdater), nameof(RegionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms));
            slop(typeof(GlowGrid), nameof(GlowGrid.GlowGridUpdate_First));
            slop(typeof(LordManager), nameof(LordManager.LordManagerUpdate));
            slop(typeof(AreaManager), nameof(AreaManager.AreaManagerUpdate));

            slop(typeof(MapDrawer), nameof(MapDrawer.WholeMapChanged));
            slop(typeof(MapDrawer), nameof(MapDrawer.MapMeshDrawerUpdate_First));
            slop(typeof(MapDrawer), nameof(MapDrawer.DrawMapMesh));
            slop(typeof(DynamicDrawManager), nameof(DynamicDrawManager.DrawDynamicThings));
            slop(typeof(GameConditionManager), nameof(GameConditionManager.GameConditionManagerDraw));
            slop(typeof(DesignationManager), nameof(DesignationManager.DrawDesignations));
            slop(typeof(OverlayDrawer), nameof(OverlayDrawer.DrawAllOverlays));
        }

        private static bool Prefix(MethodBase __originalMethod, Map map)
        {
            if (!Active)
            {
                return true;
            }

            System.Collections.Generic.List<MapComponent> components = map.components;
            int c = components.Count;
            for (int i = 0; i < c; i++)
            {
                try
                {
                    MapComponent comp = components[i];

                    Profiler prof = Modbase.Start(comp.GetType().FullName, () => $"{comp.GetType()}", null, null, null, __originalMethod);
                    comp.MapComponentUpdate();
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