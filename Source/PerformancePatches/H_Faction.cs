using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DubsAnalyzer
{
    [PerformancePatch]
    internal class H_FactionManager
    {
        public static void PerformancePatch(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(FactionManager), nameof(FactionManager.RecacheFactions)),
                new HarmonyMethod(typeof(H_FactionManager), nameof(Prefix)));

            harmony.Patch(AccessTools.Method(typeof(WorldObject), nameof(WorldObject.ExposeData)),
                new HarmonyMethod(typeof(H_FactionManager), nameof(PrefixWorldObj)));
        }

        public static void Prefix(FactionManager __instance)
        {
            if (Analyzer.Settings.FactionRemovalMode)
            {
                for (var i = 0; i < __instance.allFactions.Count; i++)
                {
                    if (__instance.allFactions[i].def == null)
                    {
                        __instance.allFactions[i].def = FactionDef.Named("OutlanderCivil");
                    }
                }
            }

        }

        public static void PrefixWorldObj(WorldObject __instance)
        {
            if (Analyzer.Settings.FactionRemovalMode)
            {
                if (Scribe.mode == LoadSaveMode.PostLoadInit)
                {
                    if (__instance.factionInt == null)
                    {
                        __instance.factionInt = Find.World.factionManager.RandomAlliedFaction();
                    }
                }
            }
        }
    }



    //[PerformancePatch]
    //internal class H_ThreadLocalDeepProfiler
    //{
    //    public static void PerformancePatch(Harmony harmony)
    //    {
    //        Analyzer.harmony.Patch(AccessTools.Method(typeof(ThreadLocalDeepProfiler), nameof(ThreadLocalDeepProfiler.Output)), new HarmonyMethod(typeof(H_ThreadLocalDeepProfiler), nameof(Prefix)));
    //    }

    //    public static bool Prefix(ThreadLocalDeepProfiler __instance, ThreadLocalDeepProfiler.Watcher root)
    //    {
    //        StringBuilder stringBuilder = new StringBuilder();
    //        if (UnityData.IsInMainThread)
    //        {
    //            stringBuilder.AppendLine("--- Main thread ---");
    //        }
    //        else
    //        {
    //            stringBuilder.AppendLine("--- Thread " + Thread.CurrentThread.ManagedThreadId + " ---");
    //        }
    //        List<ThreadLocalDeepProfiler.Watcher> list = new List<ThreadLocalDeepProfiler.Watcher>();
    //        list.Add(root);
    //        __instance.AppendStringRecursive(stringBuilder, root.Label, root.Children, root.ElapsedMilliseconds, 0, list);
    //        stringBuilder.AppendLine();
    //        stringBuilder.AppendLine();
    //        __instance.HotspotAnalysis(stringBuilder, list);
    //        Log.Message(stringBuilder.ToString(), false);

    //        return false;
    //    }

    //    public static bool Prefix(ThreadLocalDeepProfiler __instance, ThreadLocalDeepProfiler.Watcher root)
    //    {
    //        StringBuilder stringBuilder = new StringBuilder();
    //        if (UnityData.IsInMainThread)
    //        {
    //            stringBuilder.AppendLine("--- Main thread ---");
    //        }
    //        else
    //        {
    //            stringBuilder.AppendLine("--- Thread " + Thread.CurrentThread.ManagedThreadId + " ---");
    //        }
    //        List<ThreadLocalDeepProfiler.Watcher> list = new List<ThreadLocalDeepProfiler.Watcher>();
    //        list.Add(root);
    //        __instance.AppendStringRecursive(stringBuilder, root.Label, root.Children, root.ElapsedMilliseconds, 0, list);
    //        stringBuilder.AppendLine();
    //        stringBuilder.AppendLine();
    //        __instance.HotspotAnalysis(stringBuilder, list);
    //        Log.Message(stringBuilder.ToString(), false);

    //        return false;
    //    }
    //}
}