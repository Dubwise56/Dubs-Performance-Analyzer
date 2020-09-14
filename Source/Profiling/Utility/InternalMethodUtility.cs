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

        private static Harmony inst = null;
        public static Harmony Harmony => inst ??= new Harmony("InternalMethodProfiling");

        public static Dictionary<string, MethodInfo> KeyMethods = new Dictionary<string, MethodInfo>();

        /*
         * Utility
         */
        public static bool IsFunctionCall(OpCode instruction)
        {
            return (instruction == OpCodes.Call || instruction == OpCodes.Callvirt);// || instruction == OpCodes.Calli);
        }

        [HarmonyDebug]
        private static IEnumerable<CodeInstruction> Transpiler(MethodBase __originalMethod, IEnumerable<CodeInstruction> codeInstructions)
        {
            List<CodeInstruction> instructions = new List<CodeInstruction>(codeInstructions);

            for (int i = 0; i < instructions.Count(); i++)
            {
                if (IsFunctionCall(instructions[i].opcode))
                {
                    if (i == 0 || (i != 0 && instructions[i - 1].opcode != OpCodes.Constrained)) // lets ignore complicated cases
                    {
                        CodeInstruction inst = MethodTransplanting.ReplaceMethodInstruction(instructions[i], AccessTools.TypeByName(__originalMethod.Name + "-int"));
                        if (inst != instructions[i])
                        {
                            instructions[i] = inst;
                        }
                    }
                }
            }

            return instructions;
        }

    }
}
