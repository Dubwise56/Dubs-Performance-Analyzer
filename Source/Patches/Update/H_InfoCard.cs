using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Analyzer
{
    [Entry("InfoCard", Category.Update)]
    internal class H_InfoCard
    {
        public static bool Active = false;

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

        public static bool FUUUCK(Rect rect, Thing thing)
        {
            if (!Active) return true;

            Profiler prof = null;
            if (StatsReportUtility.cachedDrawEntries.NullOrEmpty<StatDrawEntry>())
            {
                prof = Analyzer.Start("SpecialDisplayStats");
                StatsReportUtility.cachedDrawEntries.AddRange(thing.def.SpecialDisplayStats(StatRequest.For(thing)));
                prof.Stop();

                prof = Analyzer.Start("StatsToDraw");
                StatsReportUtility.cachedDrawEntries.AddRange(StatsReportUtility.StatsToDraw(thing).Where(s => s.ShouldDisplay));
                prof.Stop();

                prof = Analyzer.Start("RemoveAll");
                StatsReportUtility.cachedDrawEntries.RemoveAll((StatDrawEntry de) => de.stat != null && !de.stat.showNonAbstract);
                prof.Stop();

                prof = Analyzer.Start("FinalizeCachedDrawEntries");
                StatsReportUtility.FinalizeCachedDrawEntries(StatsReportUtility.cachedDrawEntries);
                prof.Stop();
            }
            prof = Analyzer.Start("DrawStatsWorker");
            StatsReportUtility.DrawStatsWorker(rect, thing, null);
            prof.Stop();

            return false;
        }

        public static void ProfilePatch()
        {
            HarmonyMethod go = new HarmonyMethod(typeof(H_InfoCard), nameof(Start));
            HarmonyMethod biff = new HarmonyMethod(typeof(H_InfoCard), nameof(Stop));

            void slop(Type e, string s, Type[] ypes = null)
            {
                try
                {
                    Modbase.Harmony.Patch(AccessTools.Method(e, s, ypes), go, biff);
                }
                catch (Exception exception)
                {
                    Log.Error($"{s} {ypes} didn't patch\n{exception}");
                }

            }

            Modbase.Harmony.Patch(
                AccessTools.Method(typeof(StatsReportUtility), nameof(StatsReportUtility.DrawStatsReport), new[] { typeof(Rect), typeof(Thing) }),
                new HarmonyMethod(typeof(H_InfoCard), nameof(FUUUCK))
                );

            slop(typeof(Dialog_InfoCard), nameof(Dialog_InfoCard.DoWindowContents));
            slop(typeof(Dialog_InfoCard), nameof(Dialog_InfoCard.FillCard));
            slop(typeof(Dialog_InfoCard), nameof(Dialog_InfoCard.DefsToHyperlinks), new[] { typeof(IEnumerable<ThingDef>) });
            slop(typeof(Dialog_InfoCard), nameof(Dialog_InfoCard.DefsToHyperlinks), new[] { typeof(IEnumerable<DefHyperlink>) });
            slop(typeof(Dialog_InfoCard), nameof(Dialog_InfoCard.TitleDefsToHyperlinks));
            slop(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats));

            slop(typeof(StatsReportUtility), nameof(StatsReportUtility.StatsToDraw), new[] { typeof(Def), typeof(ThingDef) });
            slop(typeof(StatsReportUtility), nameof(StatsReportUtility.StatsToDraw), new[] { typeof(RoyalTitleDef), typeof(Faction) });
            slop(typeof(StatsReportUtility), nameof(StatsReportUtility.StatsToDraw), new[] { typeof(Faction) });
            slop(typeof(StatsReportUtility), nameof(StatsReportUtility.StatsToDraw), new[] { typeof(AbilityDef) });
            slop(typeof(StatsReportUtility), nameof(StatsReportUtility.StatsToDraw), new[] { typeof(Thing) });
            slop(typeof(StatsReportUtility), nameof(StatsReportUtility.StatsToDraw), new[] { typeof(WorldObject) });

            //   slop(typeof(StatsReportUtility), nameof(StatsReportUtility.DrawStatsReport), new[] { typeof(Rect), typeof(Def), typeof(ThingDef) });
            //   slop(typeof(StatsReportUtility), nameof(StatsReportUtility.DrawStatsReport), new[] { typeof(Rect), typeof(AbilityDef) });
            // slop(typeof(StatsReportUtility), nameof(StatsReportUtility.DrawStatsReport), new[] { typeof(Rect), typeof(Thing) });
            //  slop(typeof(StatsReportUtility), nameof(StatsReportUtility.DrawStatsReport), new[] { typeof(Rect), typeof(WorldObject) });
            //  slop(typeof(StatsReportUtility), nameof(StatsReportUtility.DrawStatsReport), new[] { typeof(Rect), typeof(RoyalTitleDef), typeof(Faction) });
            //  slop(typeof(StatsReportUtility), nameof(StatsReportUtility.DrawStatsReport), new[] { typeof(Rect), typeof(Faction) });

            slop(typeof(StatsReportUtility), nameof(StatsReportUtility.DrawStatsWorker));
            slop(typeof(StatsReportUtility), nameof(StatsReportUtility.FinalizeCachedDrawEntries));
        }
    }
}