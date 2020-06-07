using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace DubsAnalyzer
{
    [ProfileMode("InfoCard", UpdateMode.Update)]
    internal class H_InfoCard
    {
        public static bool Active = false;

        [HarmonyPriority(Priority.Last)]
        public static void Start(object __instance, MethodBase __originalMethod, ref string __state)
        {
            if (!Active || !AnalyzerState.CurrentlyRunning) return;
            __state = string.Empty;
            if (__instance != null)
            {
                __state = $"{__instance.GetType().Name}.{__originalMethod.Name}";
            }
            else
            if (__originalMethod.ReflectedType != null)
            {
                __state = $"{__originalMethod.ReflectedType.Name}.{__originalMethod.Name}";
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

        public static bool FUUUCK(Rect rect, Thing thing)
        {
            if (!Active || !AnalyzerState.CurrentlyRunning) return true;

            if (StatsReportUtility.cachedDrawEntries.NullOrEmpty<StatDrawEntry>())
            {
                Analyzer.Start("SpecialDisplayStats");
                StatsReportUtility.cachedDrawEntries.AddRange(thing.def.SpecialDisplayStats(StatRequest.For(thing)));
                Analyzer.Stop("SpecialDisplayStats");

                Analyzer.Start("StatsToDraw");
                StatsReportUtility.cachedDrawEntries.AddRange(from r in StatsReportUtility.StatsToDraw(thing)
                                                              where r.ShouldDisplay
                                                              select r);
                Analyzer.Stop("StatsToDraw");

                Analyzer.Start("RemoveAll");
                StatsReportUtility.cachedDrawEntries.RemoveAll((StatDrawEntry de) => de.stat != null && !de.stat.showNonAbstract);
                Analyzer.Stop("RemoveAll");

                Analyzer.Start("FinalizeCachedDrawEntries");
                StatsReportUtility.FinalizeCachedDrawEntries(StatsReportUtility.cachedDrawEntries);
                Analyzer.Stop("FinalizeCachedDrawEntries");
            }
            Analyzer.Start("DrawStatsWorker");
            StatsReportUtility.DrawStatsWorker(rect, thing, null);
            Analyzer.Stop("DrawStatsWorker");

            return false;
        }

        public static void ProfilePatch()
        {
            var go = new HarmonyMethod(typeof(H_InfoCard), nameof(Start));
            var biff = new HarmonyMethod(typeof(H_InfoCard), nameof(Stop));

            void slop(Type e, string s, Type[] ypes = null)
            {
                try
                {
                    Analyzer.harmony.Patch(AccessTools.Method(e, s, ypes), go, biff);
                }
                catch (Exception exception)
                {
                    Log.Error($"{s} {ypes} didn't patch\n{exception}");
                }

            }

            Analyzer.harmony.Patch(
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