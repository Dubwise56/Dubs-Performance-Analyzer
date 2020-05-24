using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace DubsAnalyzer
{
    [PerformancePatch]
    static class H_ComfortableTemperatureRange
    {

        public static void PerformancePatch(Harmony harmony)
        {
            var jiff = AccessTools.Method(typeof(GenTemperature), nameof(GenTemperature.ComfortableTemperatureRange), new[] { typeof(Pawn) });
            var pre = new HarmonyMethod(typeof(H_ComfortableTemperatureRange), nameof(Prefix));
            var post = new HarmonyMethod(typeof(H_ComfortableTemperatureRange), nameof(Postfix));
           harmony.Patch(jiff, pre, post);
        }

        public static Dictionary<int, FloatRange> tempCache = new Dictionary<int, FloatRange>();
        public static int LastTick = 0;
        public static bool Prefix(Pawn p, ref FloatRange __result)
        {
            if (!Analyzer.Settings.FixGame)
            {
                return true;
            }

            //if (TryIssueJobPackage.giver == null)
            //{
            //    return true;
            //}

            //__state = TryIssueJobPackage.key + ": SafeTemperatureRange";
            //Analyzer.Start(__state);

            if (LastTick == Find.TickManager.TicksGame)
            {
              //  Log.Warning("cleared", true);
               // tempCache.Clear();
                
            }
            else
            {
                LastTick = Find.TickManager.TicksGame;
                tempCache.Clear();
            }

            if (tempCache.ContainsKey(p.thingIDNumber))
            {
              //  Log.Warning("cached temp used", true);
                __result = tempCache[p.thingIDNumber];
                return false;
            }

            return true;
        }

        public static void Postfix(Pawn p, FloatRange __result)
        {
            if (!Analyzer.Settings.FixGame)
            {
                return;
            }

            if (!tempCache.ContainsKey(p.thingIDNumber))
            {
                tempCache.Add(p.thingIDNumber, __result);
            }
            //if (!string.IsNullOrEmpty(__state))
            //{
            //    Analyzer.Stop(__state);
            //}
        }
    }
}