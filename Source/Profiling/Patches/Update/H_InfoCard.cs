using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    [Entry("entry.update.infocard", Category.Update)]
    internal class H_InfoCard
    {
        public static bool Active = false;

        public static IEnumerable<MethodInfo> GetPatchMethods()
        {
            yield return AccessTools.Method(typeof(Dialog_InfoCard), nameof(Dialog_InfoCard.DoWindowContents));
            yield return AccessTools.Method(typeof(Dialog_InfoCard), nameof(Dialog_InfoCard.FillCard));
            yield return AccessTools.Method(typeof(Dialog_InfoCard), nameof(Dialog_InfoCard.DefsToHyperlinks), new[] { typeof(IEnumerable<ThingDef>) });
            yield return AccessTools.Method(typeof(Dialog_InfoCard), nameof(Dialog_InfoCard.DefsToHyperlinks), new[] { typeof(IEnumerable<DefHyperlink>) });
            yield return AccessTools.Method(typeof(Dialog_InfoCard), nameof(Dialog_InfoCard.TitleDefsToHyperlinks));
            yield return AccessTools.Method(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats));

            yield return AccessTools.Method(typeof(StatsReportUtility), nameof(StatsReportUtility.StatsToDraw), new[] { typeof(Def), typeof(ThingDef) });
            yield return AccessTools.Method(typeof(StatsReportUtility), nameof(StatsReportUtility.StatsToDraw), new[] { typeof(RoyalTitleDef), typeof(Faction) });
            yield return AccessTools.Method(typeof(StatsReportUtility), nameof(StatsReportUtility.StatsToDraw), new[] { typeof(Faction) });
            yield return AccessTools.Method(typeof(StatsReportUtility), nameof(StatsReportUtility.StatsToDraw), new[] { typeof(AbilityDef) });
            yield return AccessTools.Method(typeof(StatsReportUtility), nameof(StatsReportUtility.StatsToDraw), new[] { typeof(Thing) });
            yield return AccessTools.Method(typeof(StatsReportUtility), nameof(StatsReportUtility.StatsToDraw), new[] { typeof(WorldObject) });

            yield return AccessTools.Method(typeof(StatsReportUtility), nameof(StatsReportUtility.DrawStatsWorker));
            yield return AccessTools.Method(typeof(StatsReportUtility), nameof(StatsReportUtility.FinalizeCachedDrawEntries));
        }

        private static MethodInfo drawStatsReport;

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            instructions = CallSiteProfiling.WrapCalls(instructions, __originalMethod,
                m => m.Name == nameof(ThingDef.SpecialDisplayStats) && m.GetParameters() is [{ ParameterType: var p }] && p == typeof(StatRequest),
                AccessTools.Method(typeof(H_InfoCard), nameof(SpecialDisplayStats)));
            instructions = CallSiteProfiling.WrapCalls(instructions, __originalMethod,
                AccessTools.Method(typeof(StatsReportUtility), nameof(StatsReportUtility.StatsToDraw), new[] { typeof(Thing) }),
                AccessTools.Method(typeof(H_InfoCard), nameof(StatsToDraw)));
            instructions = CallSiteProfiling.WrapCalls(instructions, __originalMethod,
                AccessTools.Method(typeof(List<StatDrawEntry>), nameof(List<StatDrawEntry>.RemoveAll)),
                AccessTools.Method(typeof(H_InfoCard), nameof(RemoveAll)));
            instructions = CallSiteProfiling.WrapCalls(instructions, __originalMethod,
                AccessTools.Method(typeof(StatsReportUtility), nameof(StatsReportUtility.FinalizeCachedDrawEntries)),
                AccessTools.Method(typeof(H_InfoCard), nameof(FinalizeCachedDrawEntries)));
            return CallSiteProfiling.WrapCalls(instructions, __originalMethod,
                AccessTools.Method(typeof(StatsReportUtility), nameof(StatsReportUtility.DrawStatsWorker)),
                AccessTools.Method(typeof(H_InfoCard), nameof(DrawStatsWorker)));
        }

        public static IEnumerable<StatDrawEntry> SpecialDisplayStats(Def def, StatRequest req)
        {
            if (!Active) return def.SpecialDisplayStats(req);

            var prof = ProfileController.Start("SpecialDisplayStats", null, null, drawStatsReport);
            var entries = def.SpecialDisplayStats(req).ToList();
            prof?.Stop();
            return entries;
        }

        public static IEnumerable<StatDrawEntry> StatsToDraw(Thing thing)
        {
            if (!Active) return StatsReportUtility.StatsToDraw(thing);

            var prof = ProfileController.Start("StatsToDraw", null, null, drawStatsReport);
            var entries = StatsReportUtility.StatsToDraw(thing).ToList();
            prof?.Stop();
            return entries;
        }

        public static int RemoveAll(List<StatDrawEntry> list, Predicate<StatDrawEntry> match)
        {
            if (!Active) return list.RemoveAll(match);

            var prof = ProfileController.Start("RemoveAll", null, null, drawStatsReport);
            var removed = list.RemoveAll(match);
            prof?.Stop();
            return removed;
        }

        public static void FinalizeCachedDrawEntries(IEnumerable<StatDrawEntry> original)
        {
            if (!Active)
            {
                StatsReportUtility.FinalizeCachedDrawEntries(original);
                return;
            }

            var prof = ProfileController.Start("FinalizeCachedDrawEntries", null, null, drawStatsReport);
            StatsReportUtility.FinalizeCachedDrawEntries(original);
            prof?.Stop();
        }

        public static void DrawStatsWorker(Rect rect, Thing optionalThing, WorldObject optionalWorldObject)
        {
            if (!Active)
            {
                StatsReportUtility.DrawStatsWorker(rect, optionalThing, optionalWorldObject);
                return;
            }

            var prof = ProfileController.Start("DrawStatsWorker", null, null, drawStatsReport);
            StatsReportUtility.DrawStatsWorker(rect, optionalThing, optionalWorldObject);
            prof?.Stop();
        }

        public static void ProfilePatch()
        {
            MethodTransplanting.PatchMethods(typeof(H_InfoCard));

            drawStatsReport = AccessTools.Method(typeof(StatsReportUtility), nameof(StatsReportUtility.DrawStatsReport), new[] { typeof(Rect), typeof(Thing) });
            Modbase.Harmony.Patch(drawStatsReport, transpiler: new HarmonyMethod(typeof(H_InfoCard), nameof(Transpiler)));
        }
    }
}