using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Analyzer.Profiling;

using HarmonyLib;

using RimWorld;
using RimWorld.Planet;

using Verse;

namespace Analyzer.Fixes
{
	class H_FactionManager : Fix
	{
		public static bool Active = false;
		public override string Name => "fix.faction";

		public override void OnGameInit(Game g, Harmony h)
		{
			var skiff = AccessTools.Method(typeof(WorldObjectsHolder), nameof(WorldObjectsHolder.ExposeData));
			h.Patch(skiff, new HarmonyMethod(typeof(H_FactionManager), nameof(Prefix)));
		}

		public static bool Prefix(WorldObjectsHolder __instance)
		{
			if (!Active) return true;
			__instance.worldObjects.RemoveAll(x => x?.Faction == null);
			__instance.Recache();
			return true;
		}

		public override void OnGameLoaded(Game g, Harmony h)
		{
			var factionManger = g.World.factionManager;

			if (factionManger == null)
			{
				Log.Error("null faction manager?");
				return;
			}

			g.World.worldObjects.worldObjects.RemoveAll(x => x?.Faction == null);
			g.World.worldObjects.Recache();

			foreach (var fac in factionManger.allFactions.Where(f => f != null && f.def == null))
			{
				fac.def = FactionDef.Named("OutlanderCivil");
			}


			//try
			//{

			//	foreach (var wo in g.World.worldObjects.worldObjects.Where(w => w.factionInt.def == null))
			//	{
			//		var faction = factionManger.allFactions.Where(f => !f.IsPlayer && (!f.hidden ?? true)).RandomElement();

			//		ThreadSafeLogger.Warning($"[Analyzer] Changed the world object {wo.Label}'s faction from {wo.factionInt.name}(Removed) to {faction.name}");
			//		wo.factionInt = faction;
			//	}
			//}
			//catch (Exception e)
			//{
			//	Log.Error("changed world object faction name failed"+e.ToString());
			//}

			//try
			//{
			//	foreach (var wp in g.World.worldPawns.AllPawnsAliveOrDead.Where(p => p.factionInt.def == null))
			//	{
			//		var faction = factionManger.allFactions.Where(f => !f.IsPlayer && (!f.hidden ?? true)).RandomElement();

			//		ThreadSafeLogger.Warning($"[Analyzer] Changed the pawn {wp.Label}'s faction from {wp.factionInt.name}(Removed) to {faction.name}");
			//		wp.factionInt = faction;
			//	}
			//}
			//catch (Exception e)
			//{
			//	Log.Error("pawn faction name change failed"+e.ToString());
			//}

			//    Active = false;
		}
	}
}