using HarmonyLib;

using RimWorld;

using Verse;
using Verse.AI;

namespace Analyzer.Profiling
{
	[Entry("entry.tick.jobdrivertick", Category.Tick)]
	internal class H_JobTrackerTick
	{
		[Setting("Per Pawn")] public static bool PerPawn = false;

		public static bool Active = false;

		public static void ProfilePatch()
		{
			var pre = new HarmonyMethod(typeof(H_JobTrackerTick), nameof(Prefix));
			var o = AccessTools.Method(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.JobTrackerTick));
			Modbase.Harmony.Patch(o, pre);
		}

		private static bool Prefix(Pawn_JobTracker __instance)
		{
			if (!Active)
			{
				return true;
			}

			Detour(__instance);
			return false;
		}

		private static void Detour(Pawn_JobTracker __instance)
		{
			Profiler pro = null;
			__instance.jobsGivenThisTick = 0;
			__instance.jobsGivenThisTickTextual = "";
			if (__instance.pawn.IsHashIntervalTick(30))
			{
				pro = ProfileController.Start("DetermineNextConstantThinkTreeJob");
				var thinkResult = __instance.DetermineNextConstantThinkTreeJob();
				pro.Stop();
				if (thinkResult.IsValid)
				{
					pro = ProfileController.Start("ShouldStartJobFromThinkTree");
					if (__instance.ShouldStartJobFromThinkTree(thinkResult))
					{
						__instance.CheckLeaveJoinableLordBecauseJobIssued(thinkResult);
						__instance.StartJob(thinkResult.Job, JobCondition.InterruptForced, thinkResult.SourceNode,
							false, false, __instance.pawn.thinker.ConstantThinkTree, thinkResult.Tag);
					}
					else if (thinkResult.Job != __instance.curJob && !__instance.jobQueue.Contains(thinkResult.Job))
					{
						JobMaker.ReturnToPool(thinkResult.Job);
					}

					pro.Stop();
				}
			}

			if (__instance.curDriver != null)
			{
				if (__instance.curJob.expiryInterval > 0 &&
					(Find.TickManager.TicksGame - __instance.curJob.startTick) % __instance.curJob.expiryInterval ==
					0 && Find.TickManager.TicksGame != __instance.curJob.startTick)
				{
					pro = ProfileController.Start("EnemiesAreNearby");
					var enemies = !__instance.curJob.expireRequiresEnemiesNearby ||
								  PawnUtility.EnemiesAreNearby(__instance.pawn, 25);
					pro.Stop();
					if (enemies)
					{
						if (__instance.debugLog)
						{
							__instance.DebugLogEvent("Job expire");
						}

						if (!__instance.curJob.checkOverrideOnExpire)
						{
							pro = ProfileController.Start("EndCurrentJob");
							__instance.EndCurrentJob(JobCondition.Succeeded);
							pro.Stop();
						}
						else
						{
							pro = ProfileController.Start("CheckForJobOverride");
							__instance.CheckForJobOverride();
							pro.Stop();
						}

						pro = ProfileController.Start("FinalizeTick");
						__instance.FinalizeTick();
						pro.Stop();
						return;
					}

					if (__instance.debugLog)
					{
						__instance.DebugLogEvent("Job expire skipped because there are no enemies nearby");
					}
				}

				var key = string.Empty;

				if (PerPawn)
				{
					key = __instance.pawn.LabelShort + __instance.curDriver.job?.def.defName;
				}
				else
				{
					key = __instance.curDriver.job?.def.defName;
				}

				string label()
				{
					var daffy = string.Empty;
					if (PerPawn)
					{
						daffy =
							$"{__instance.pawn.LabelShort} {__instance.curDriver.job?.def?.defName} - {__instance.curDriver.job?.def?.driverClass} - {__instance.curDriver.job?.def?.modContentPack?.Name}";
					}
					else
					{
						daffy =
							$"{__instance.curDriver.job?.def?.defName} - {__instance.curDriver.job?.def?.driverClass} - {__instance.curDriver.job?.def?.modContentPack?.Name}";
					}

					return daffy;
				}

				var meth = AccessTools.Method(__instance.curDriver.job?.def?.driverClass, "DriverTick");

				pro = ProfileController.Start(key, label, __instance.curDriver.job?.def?.driverClass,
					__instance.curDriver.job?.def, __instance.pawn, meth);
				__instance.curDriver.DriverTick();
				pro.Stop();
			}

			if (__instance.curJob == null && !__instance.pawn.Dead && __instance.pawn.mindState.Active)
			{
				if (__instance.debugLog)
				{
					__instance.DebugLogEvent("Starting job from Tick because curJob == null.");
				}

				pro = ProfileController.Start("FinalizeTick");
				__instance.TryFindAndStartJob();
				pro.Stop();
			}
			pro = ProfileController.Start("FinalizeTick");
			__instance.FinalizeTick();
			pro.Stop();
		}
	}
}