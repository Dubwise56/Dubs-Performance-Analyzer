using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Analyzer.Profiling.Patches.Tick
{
    [Entry("entry.tick.workgiver", Category.Tick)]
    public static class H_TryIssueJobPackageTrans
    {
        [Setting("By Work Type")] 
        public static bool ByWorkType = false;

        [Setting("Request Types")] 
        public static bool RequestTypes = false;

        [Setting("Per Pawn")] 
        public static bool PerPawn = false;

        public static bool Active = false;
        public static Dictionary<WorkGiver, MethodInfo> cachedMethods = new Dictionary<WorkGiver, MethodInfo>();

        public static void ProfilePatch()
        {
            Modbase.Harmony.Patch(AccessTools.Method(typeof(JobGiver_Work), nameof(JobGiver_Work.TryIssueJobPackage)),
                transpiler: new HarmonyMethod(typeof(H_TryIssueJobPackageTrans), nameof(Transpiler)));
        }


        public static Profiler Start(WorkGiver giver, Pawn p)
        {
            if (!Active) return null;

            var key = "";

            key = ByWorkType ? giver.def.workType.defName : giver.def.defName;
            if (PerPawn) key += p?.Name?.ToStringShort;

            if (cachedMethods.TryGetValue(giver, out var meth) == false)
            {
                if (giver is WorkGiver_Scanner)
                {
                    if (giver.def.scanThings) meth = AccessTools.Method(giver.GetType(), "PotentialWorkThingsGlobal");
                    else meth = AccessTools.Method(giver.GetType(), "PotentialWorkCellsGlobal");
                }
                else
                {
                    meth = AccessTools.Method(giver.GetType(), "NonScanJob");
                }
                cachedMethods.Add(giver, meth);
            }

            return ProfileController.Start(key, () =>
            {
                var label = "";

                if (ByWorkType) label = giver.def?.workType?.defName;
                else
                {
                    label = $"{giver.def?.defName} - {giver.def?.workType?.defName} - {giver.def?.modContentPack?.Name}";

                    if (RequestTypes && giver is WorkGiver_Scanner scan)
                    {
                        label += $" - {scan.PotentialWorkThingRequest}";
                        if (scan.PotentialWorkThingRequest.group == ThingRequestGroup.BuildingArtificial)
                        {
                            label += " VERY BAD!";
                        }
                    }
                }

                if (PerPawn) label += $" - {p?.Name?.ToStringShort}";

                return label;
            }, null, meth);
        }

        public static void Stop(Profiler profiler)
        {
            if (Active)
            {
                profiler?.Stop();
            }
        }

        /* Our workgiver is the local at location 8: `[8] class RimWorld.WorkGiver,`
         * 
         * The initial insertion is the line before `Job job = workGiver.NonScanJob(pawn);`
         * The 'final' is the line before `if (bestTargetOfLastPriority.IsValid)`
         * 
         * The goal is inserting a local of the type 'Profiler' assigning it the value of Start(WorkGiver giver);
         */

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
        {
            var instructionsList = instructions.ToList();
            var local = ilGen.DeclareLocal(typeof(Profiler));

            bool start = false;
            bool endloop = false;

            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction instruction = instructionsList[i];

                if (start == false && instruction.opcode == OpCodes.Nop)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_S, (byte)8);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(H_TryIssueJobPackageTrans), nameof(Start)));
                    yield return new CodeInstruction(OpCodes.Stloc, local.LocalIndex);
                    yield return instruction;
                    start = true;
                }
                else if (endloop == false && MatchMethod(instructionsList, i))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc, local.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(H_TryIssueJobPackageTrans), nameof(Stop)));
                    yield return instruction;
                    endloop = true;
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        private static bool MatchMethod(List<CodeInstruction> list, int index)
        {
            return index < list.Count - 2 &&
                   (list[index].opcode == OpCodes.Ldflda &&
                    list[index - 1].opcode == OpCodes.Ldloc_0 &&
                    list[index - 2].opcode == OpCodes.Endfinally &&
                    list[index - 3].opcode == OpCodes.Leave_S);
        }
    }
}
