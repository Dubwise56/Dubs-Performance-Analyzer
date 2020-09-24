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
        public static Dictionary<string, MethodInfo> transpiledMethods = new Dictionary<string, MethodInfo>();

        public static void ClearCaches()
        {
            PatchedInternals.Clear();
            transpiledMethods.Clear();
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

                            if (!transpiledMethods.ContainsKey(key))
                                transpiledMethods.Add(key, meth);

                            CodeInstruction inst = MethodTransplanting.ReplaceMethodInstruction(
                                instructions[i],
                                key,
                                AccessTools.TypeByName(__originalMethod.Name + "-int"),
                                AccessTools.Field(typeof(InternalMethodUtility), "transpiledMethods"));

                            if (inst != instructions[i]) instructions[i] = inst;
                        }
                    }
                }

                return instructions;
            }
            catch (Exception e)
            {
                ThreadSafeLogger.Error("Failed to patch internal method, failed with the error " + e.Message);
                return codeInstructions;
            }
        }


    }
}
