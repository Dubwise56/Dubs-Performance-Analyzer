using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
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

        private static IEnumerable<CodeInstruction> Transpiler(MethodBase __originalMethod, IEnumerable<CodeInstruction> codeInstructions)
        {
            try
            {
                var instructions = codeInstructions.ToList();

                for (int i = 0; i < instructions.Count; i++)
                {
                    if (!Utility.ValidCallInstruction(instructions[i], i == 0 ? null : instructions[i - 1], out var meth, out var key)) continue;
                    
                    var index = MethodInfoCache.AddMethod(key, meth);

                    var inst = MethodTransplanting.ReplaceMethodInstruction(
                        instructions[i],
                        key,
                        GUIController.types[__originalMethod.DeclaringType + ":" + __originalMethod.Name + "-int"],
                        index);

                    instructions[i] = inst;
                }

                return instructions;
            }
            catch (Exception e)
            {

#if DEBUG
                ThreadSafeLogger.ReportException(e, $"Failed to patch the methods inside {Utility.GetSignature(__originalMethod, false)}");
#else
                if(Settings.verboseLogging)
                    ThreadSafeLogger.ReportException(e, $"Failed to patch the methods inside {Utility.GetSignature(__originalMethod, false)}");
#endif

                return codeInstructions;
            }
        }
    }
}
