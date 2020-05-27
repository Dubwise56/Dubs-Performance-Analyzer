using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace DubsAnalyzer
{
	[HarmonyPatch(typeof(JobGiver_OptimizeApparel), nameof(JobGiver_OptimizeApparel.TryGiveJob))]
	public static class H_JobGiver_OptimizeApparel_TryGiveJob
	{
		public static QualityCategory tmpQualityCategory = QualityCategory.Awful;
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var myType = AccessTools.Method(typeof(StringBuilder), nameof(StringBuilder.AppendLine), new Type[] { typeof(string) });
			var mylogicmeth = AccessTools.Method(typeof(H_JobGiver_OptimizeApparel_TryGiveJob), nameof(H_JobGiver_OptimizeApparel_TryGiveJob.Logic));

			int i = int.MinValue;
			foreach (var instruction in instructions)
			{
				if (i == 2)
				{
					var inst = new CodeInstruction(OpCodes.Ldarg_1);
					inst.labels = instruction.labels;
					instruction.labels = new List<Label>();

					yield return inst; // pawn
					yield return new CodeInstruction(OpCodes.Call, mylogicmeth);
				}
				else if ((i < 0) && instruction.opcode == OpCodes.Callvirt && instruction.operand == myType)
				{
					i = 0;
				}
				i++;
				yield return instruction;
			}
		}
		public static void Logic(Pawn pawn)
		{
			if (!Analyzer.Settings.OptimiseJobGiverOptimise) return;

			if (pawn != null && pawn.royalty != null && pawn.royalty.AllTitlesInEffectForReading.Count > 0)
			{
				JobGiver_OptimizeApparel.tmpAllowedApparels.Clear();
				JobGiver_OptimizeApparel.tmpRequiredApparels.Clear();
				JobGiver_OptimizeApparel.tmpBodyPartGroupsWithRequirement.Clear();
				QualityCategory qualityCategory = QualityCategory.Awful;
				foreach (RoyalTitle item in pawn.royalty.AllTitlesInEffectForReading)
				{
					if (item.def.requiredApparel != null)
					{
						for (int i = 0; i < item.def.requiredApparel.Count; i++)
						{
							JobGiver_OptimizeApparel.tmpAllowedApparels.AddRange(item.def.requiredApparel[i].AllAllowedApparelForPawn(pawn, ignoreGender: false, includeWorn: true));
							JobGiver_OptimizeApparel.tmpRequiredApparels.AddRange(item.def.requiredApparel[i].AllRequiredApparelForPawn(pawn, ignoreGender: false, includeWorn: true));
							JobGiver_OptimizeApparel.tmpBodyPartGroupsWithRequirement.AddRange(item.def.requiredApparel[i].bodyPartGroupsMatchAny);
						}
					}
					if ((int)item.def.requiredMinimumApparelQuality > (int)qualityCategory)
					{
						tmpQualityCategory = item.def.requiredMinimumApparelQuality;
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(JobGiver_OptimizeApparel), nameof(JobGiver_OptimizeApparel.ApparelScoreRaw))]
	[HarmonyDebug]
	public static class H_Jobgiver_OptimizeApparel_ScoreRaw
	{

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var instList = instructions.ToList();
			var targetFunc = AccessTools.Method(typeof(List<RoyalTitle>), "get_Count");
			var inActiveFunc = AccessTools.Method(typeof(H_Jobgiver_OptimizeApparel_ScoreRaw), "IsInactive");
			var qual = AccessTools.Field(typeof(H_JobGiver_OptimizeApparel_TryGiveJob), "tmpQualityCategory");

			bool HasSeenFirst = false;
			bool HasAddedSkip = false;
			bool HasSeenSecond = false;
			var skipLabel = generator.DefineLabel();

			for (int i = 0; i < instList.Count(); i++)
			{
				if(HasSeenSecond)
				{
					yield return instList[i];
					continue;
				}

				if(!HasSeenFirst && instList[i].opcode == OpCodes.Callvirt && instList[i].operand == targetFunc)
				{
					yield return instList[i++];
					yield return instList[i++];
					yield return instList[i++];
					HasSeenFirst = true;
					yield return new CodeInstruction(OpCodes.Call, inActiveFunc);
					yield return new CodeInstruction(OpCodes.Brfalse, skipLabel);
				}
				if(HasSeenFirst && !HasAddedSkip && instList[i].opcode == OpCodes.Endfinally && instList[i-2].opcode == OpCodes.Constrained)
				{
					instList[i + 1].labels.Add(skipLabel);
					HasAddedSkip = true;
				}
				if (HasAddedSkip && !HasSeenSecond && instList[i].opcode == OpCodes.Ldloc_S && instList[i - 1].opcode == OpCodes.Ldloc_S)
				{
					// we are doing a tenary :-)
					var trueLabel = generator.DefineLabel();
					var endLabel = generator.DefineLabel();

					// this is equivalent to IsActive() ? ldloc_5 : H_JobGiver_OptimizeApparel_TryGiveJob.tmpQualityCategory

					yield return new CodeInstruction(OpCodes.Call, inActiveFunc); // get our bool on the stack
					yield return new CodeInstruction(OpCodes.Brtrue, trueLabel); // is it true? lets move to that.
					yield return instList[i]; // if it is false, we stick with what we had originally
					yield return new CodeInstruction(OpCodes.Br, endLabel); // if we had false, goto the next section of code
					var ldfldInst = new CodeInstruction(OpCodes.Ldsfld, qual); // if it is true, get our alt logic
					ldfldInst.labels.Add(trueLabel); // make sure, if we skip, we skip to the correct il
					yield return ldfldInst;
					instList[i + 1].labels.Add(endLabel); 
					HasSeenSecond = true;
				}
				else
				{
					yield return instList[i];
				}

			}


		}

		// the pinnacle of laziness, instead of accessing Analyzer.Settings.OptimiseJobGiverOptimise
		// we do it through a method :D ~Wiri
		public static bool IsInactive()
		{
			return !Analyzer.Settings.OptimiseJobGiverOptimise;
		}
	}
}




