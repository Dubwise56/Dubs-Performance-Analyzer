//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using HarmonyLib;
//using RimWorld;
//using RimWorld.Planet;
//using Verse;

//namespace Analyzer.Performance
//{
//    internal class H_FactionManager : PerfPatch
//    {
//        public static bool Active = false;

//        public override string Name => "performance.compdeepdrill";


//        public override void OnDisabled(Harmony harmony)
//        {
//            harmony.Patch(AccessTools.Method(typeof(FactionManager), nameof(FactionManager.RecacheFactions)),
//                new HarmonyMethod(typeof(H_FactionManager), nameof(Prefix)));

//            harmony.Patch(AccessTools.Method(typeof(WorldObject), nameof(WorldObject.ExposeData)),
//                new HarmonyMethod(typeof(H_FactionManager), nameof(PrefixWorldObj)));
//        }


//        public static void Prefix(FactionManager __instance)
//        {
//            if (Analyzer.Settings.FactionRemovalMode)
//            {
//                for (var i = 0; i < __instance.allFactions.Count; i++)
//                {
//                    if (__instance.allFactions[i]
//                        .def == null)
//                    {
//                        __instance.allFactions[i]
//                            .def = FactionDef.Named("OutlanderCivil");
//                    }
//                }
//            }
//        }

//        public static void PrefixWorldObj(WorldObject __instance)
//        {
//            if (Analyzer.Settings.FactionRemovalMode)
//            {
//                if (Scribe.mode == LoadSaveMode.PostLoadInit)
//                {
//                    if (__instance.factionInt == null)
//                    {
//                        __instance.factionInt = Find.World.factionManager.RandomAlliedFaction();
//                    }
//                }
//            }
//        }
//    }
//}