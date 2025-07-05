using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Analyzer.Profiling
{
    public class CodeInstMethEqual : EqualityComparer<CodeInstruction>
    {
        // Functions primarily to check if two function call CodeInstructions are the same. 
        public override bool Equals(CodeInstruction a, CodeInstruction b)
        {
            if (a.opcode != b.opcode) return false;
                
            // because our previous check, both must be the same opcode.
            if (a.opcode == OpCodes.Callvirt || a.opcode == OpCodes.Call)
            {
                return (a.operand as MethodBase)?.Name == (b.operand as MethodBase)?.Name;
            }

            return a.operand == b.operand;
        }

        public override int GetHashCode(CodeInstruction obj)
        {
            return obj.GetHashCode();
        }
    }


    public static class TranspilerMethodUtility
    {
        public static HarmonyMethod TranspilerProfiler = new HarmonyMethod(typeof(TranspilerMethodUtility), nameof(Transpiler));

        public static List<MethodBase> PatchedMeths = new List<MethodBase>();
        public static CodeInstMethEqual methComparer = new CodeInstMethEqual();

        // Clear the caches which prevent double patching
        public static void ClearCaches()
        {
            PatchedMeths.Clear();
#if DEBUG
            ThreadSafeLogger.Message("[Analyzer] Cleaned up the transpiler methods caches");
#endif
        }

        /* This method takes a method, and computes the different in CodeInstructions,
         * from this difference, it then looks at all 'added' instructions that are either
         * `Call` or `CallVirt` instructions, and attempts to swap the MethodInfo which
         * is called from the instruction, the method it gets switched to is created at 
         * runtime in `MethodTransplating.ReplaceMethodInstruction`
         */

        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(MethodBase __originalMethod, IEnumerable<CodeInstruction> instructions)
        {
            var inst = PatchProcessor.GetOriginalInstructions(__originalMethod);
            var modInstList = instructions.ToList();

            var insts = new Myers<CodeInstruction>(inst.ToArray(), modInstList.ToArray(), methComparer);
            insts.Compute();

            // var key = Utility.GetMethodKey(__originalMethod);
            // var index = MethodInfoCache.AddMethod(key, __originalMethod);

            foreach (var thing in insts.changeSet)
            {
                // We only want added methods
                if (thing.change != ChangeType.Added) continue;

                var instruction = thing.value;
                if (!Utility.ValidCallInstruction(instruction, null, out var meth, out var key)) continue;
                if (!(meth is MethodInfo)) continue;

                var index = MethodInfoCache.AddMethod(key, meth);
                
                // swap our instruction
                var replaceInstruction = MethodTransplanting.ReplaceMethodInstruction(
                    instruction,
                    key,
                    typeof(H_HarmonyTranspilersInternalMethods),
                    index);
            
                modInstList[thing.rIndex] = replaceInstruction;
            }

            return modInstList;
        }
    }
}