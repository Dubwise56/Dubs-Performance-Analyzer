using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Verse;

namespace Analyzer.Profiling
{
    public static class InternalMethodUtility
    {
        public static HarmonyMethod InternalProfiler = new HarmonyMethod(typeof(InternalMethodUtility), nameof(InternalMethodUtility.Transpiler));

        public static HashSet<MethodInfo> PatchedInternals = new HashSet<MethodInfo>();

        public static void ClearCaches()
        {
            PatchedInternals.Clear();

#if DEBUG
            ThreadSafeLogger.Message("[Analyzer] Cleaned up the internal method caches");
#endif
        }

        public static bool IsFunctionCall(OpCode instruction)
        {
            return (instruction == OpCodes.Call || instruction == OpCodes.Callvirt);// || instruction == OpCodes.Calli);
        }

        private static IEnumerable<CodeInstruction> Transpiler(MethodBase __originalMethod, IEnumerable<CodeInstruction> codeInstructions)
        {
            try
            {
                List<CodeInstruction> instructions = new List<CodeInstruction>(codeInstructions);

                for (int i = 0; i < instructions.Count(); i++)
                {

                    if (IsFunctionCall(instructions[i].opcode))
                    {
                        if (i == 0 || instructions[i - 1].opcode != OpCodes.Constrained)
                        {
                            MethodInfo meth = null;
                            try { meth = instructions[i].operand as MethodInfo; } catch { }
                            if (meth == null) continue;

                            var key = meth.DeclaringType.FullName + "." + meth.Name;
                            var index = MethodInfoCache.AddMethod(key, meth);


                            CodeInstruction inst = MethodTransplanting.ReplaceMethodInstruction(
                                instructions[i],
                                key,
                                GUIController.types[__originalMethod.Name + "-int"],
                                index);

                            instructions[i] = inst;
                        }
                    }
                }

                return instructions;
            }
            catch (Exception e)
            {
                ThreadSafeLogger.Error($"Failed to patch the internal method {__originalMethod.DeclaringType.FullName}:{__originalMethod.Name}, failed with the error " + e.Message);
                return codeInstructions;
            }
        }


    }
}
