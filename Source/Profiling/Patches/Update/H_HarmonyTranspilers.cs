using HarmonyLib;
using RimWorld.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    public class CodeInstMethEqual : EqualityComparer<CodeInstruction>
    {
        public override bool Equals(CodeInstruction b1, CodeInstruction b2)
        {
            if (b1.opcode != b2.opcode) return false;

            if (InternalMethodUtility.IsFunctionCall(b1.opcode))
            {
                try
                {
                    return ((MethodInfo)b1.operand).Name == ((MethodInfo)b2.operand).Name;
                }
                catch { }
            }

            return b1.operand == b2.operand;
        }

        public override int GetHashCode(CodeInstruction obj)
        {
            return obj.GetHashCode();
        }
    }

    [Entry("HarmonyTranspilers", Category.Update, "TransPatchTipKey")]
    public static class H_HarmonyTranspilers
    {
        public static bool Active = false;
        public static List<MethodBase> PatchedMeths = new List<MethodBase>();
        public static Dictionary<string, MethodInfo> MethodInfos = new Dictionary<string, MethodInfo>();
        public static CodeInstMethEqual methComparer = new CodeInstMethEqual();

        public static void ProfilePatch()
        {
            HarmonyMethod trans = new HarmonyMethod(typeof(H_HarmonyTranspilers), nameof(Transpiler));
            var patches = Harmony.GetAllPatchedMethods().ToList();

            var filteredTranspilers = patches
                .Where(m => Harmony.GetPatchInfo(m).Transpilers.Any(p => p.owner != Modbase.Harmony.Id && !PatchedMeths.Contains(p.PatchMethod)))
                .ToList();

            PatchedMeths.AddRange(filteredTranspilers);

            foreach (var meth in filteredTranspilers)
            {
                try
                {
                    Modbase.Harmony.Patch(meth, transpiler: trans);
                }
                catch (Exception e)
                {
#if DEBUG
                        ThreadSafeLogger.Error($"[Analyzer] Failed to patch transpiler, failed with the message {e.Message}");
#endif
#if NDEBUG
                    if (Settings.verboseLogging)
                        ThreadSafeLogger.Error($"[Analyzer] Failed to patch transpiler, failed with the message {e.Message}");
#endif
                }
            }
        }

        [HarmonyPriority(Priority.Last)]
        public static IEnumerable<CodeInstruction> Transpiler(MethodBase __originalMethod, IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> inst = PatchProcessor.GetOriginalInstructions(__originalMethod);
            List<CodeInstruction> modInstList = instructions.ToList();

            Myers<CodeInstruction> insts = new Myers<CodeInstruction>(inst.ToArray(), modInstList.ToArray(), methComparer);
            insts.Compute();

            foreach (var thing in insts.changeSet)
            {
                if (thing.change != ChangeType.Added) continue;
                if (!InternalMethodUtility.IsFunctionCall(thing.value.opcode)) continue;

                var key = __originalMethod.DeclaringType.FullName + ":" + __originalMethod.Name;
                if (!MethodInfos.ContainsKey(key))
                {
                    MethodInfos.Add(key, (MethodInfo)__originalMethod);
                }

                var replaceInstruction = MethodTransplanting.ReplaceMethodInstruction(thing.value, typeof(H_HarmonyTranspilers));

                for (int i = 0; i < modInstList.Count; i++)
                {
                    var instruction = modInstList[i];
                    if (InternalMethodUtility.IsFunctionCall(instruction.opcode))
                    {
                        if (((MethodInfo)instruction.operand).Name == ((MethodInfo)thing.value.operand).Name)
                        {
                            if (instruction != replaceInstruction)
                                modInstList[i] = replaceInstruction;
                            break;
                        }
                    }
                }
            }

            return modInstList;
        }
    }
}




