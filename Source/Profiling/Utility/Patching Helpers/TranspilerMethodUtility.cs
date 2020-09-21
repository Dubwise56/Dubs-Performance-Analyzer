using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

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


    public static class TranspilerMethodUtility
    {
        public static HarmonyMethod TranspilerProfiler = new HarmonyMethod(typeof(TranspilerMethodUtility), nameof(TranspilerMethodUtility.Transpiler));

        public static List<MethodBase> PatchedMeths = new List<MethodBase>();
        public static Dictionary<string, MethodInfo> transpiledMethods = new Dictionary<string, MethodInfo>();
        public static CodeInstMethEqual methComparer = new CodeInstMethEqual();

        public static void ClearCaches()
        {
            PatchedMeths.Clear();
            transpiledMethods.Clear();
        }

        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(MethodBase __originalMethod, IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> inst = PatchProcessor.GetOriginalInstructions(__originalMethod);
            List<CodeInstruction> modInstList = instructions.ToList();

            Myers<CodeInstruction> insts = new Myers<CodeInstruction>(inst.ToArray(), modInstList.ToArray(), methComparer);
            insts.Compute();

            transpiledMethods.Add(__originalMethod.DeclaringType.FullName + "." + __originalMethod.Name, __originalMethod as MethodInfo);

            foreach (var thing in insts.changeSet)
            {
                if (thing.change != ChangeType.Added) continue;
                if (!InternalMethodUtility.IsFunctionCall(thing.value.opcode)) continue;

                var replaceInstruction = MethodTransplanting.ReplaceMethodInstruction(
                    thing.value,
                    __originalMethod.DeclaringType.FullName + "." + __originalMethod.Name,
                    typeof(H_HarmonyTranspilers),
                    AccessTools.Field(typeof(TranspilerMethodUtility), "transpiledMethods"));

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
