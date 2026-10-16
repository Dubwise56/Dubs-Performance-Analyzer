using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Analyzer.Profiling
{
    public static class CallSiteProfiling
    {
        public static IEnumerable<CodeInstruction> WrapCalls(IEnumerable<CodeInstruction> instructions, MethodBase original,
            MethodInfo target, MethodInfo wrapper, bool passInstance = false)
            => WrapCalls(instructions, original, m => m == target, wrapper, passInstance);

        public static IEnumerable<CodeInstruction> WrapCalls(IEnumerable<CodeInstruction> instructions, MethodBase original,
            Func<MethodInfo, bool> match, MethodInfo wrapper, bool passInstance = false)
        {
            var codes = instructions.ToList();
            var wrapped = 0;

            for (var i = 0; i < codes.Count; i++)
            {
                var inst = codes[i];
                if (inst.opcode != OpCodes.Call && inst.opcode != OpCodes.Callvirt) continue;
                if (inst.operand is not MethodInfo method || !match(method)) continue;

                if (i > 0 && codes[i - 1].opcode == OpCodes.Constrained) continue;

                CheckSignature(method, wrapper, passInstance ? original.DeclaringType : null);

                if (passInstance)
                {
                    codes.Insert(i, new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(inst));
                    i++;
                }

                codes[i] = new CodeInstruction(OpCodes.Call, wrapper).MoveLabelsFrom(inst).MoveBlocksFrom(inst);
                wrapped++;
            }

            if (wrapped == 0)
                ThreadSafeLogger.Warning($"[Analyzer] {wrapper.DeclaringType?.Name}.{wrapper.Name} found no call to wrap in {Utility.GetSignature(original, false)}, the game version probably changed it");

            return codes;
        }

        private static void CheckSignature(MethodInfo method, MethodInfo wrapper, Type instanceType)
        {
            var expected = method.GetParameters().Select(p => p.ParameterType);
            if (!method.IsStatic)
                expected = expected.Prepend(method.DeclaringType!.IsValueType ? method.DeclaringType.MakeByRefType() : method.DeclaringType);
            if (instanceType != null)
                expected = expected.Append(instanceType);

            var e = expected.ToArray();
            var a = wrapper.GetParameters().Select(p => p.ParameterType).ToArray();
            if (!wrapper.IsStatic || wrapper.ReturnType != method.ReturnType || e.Length != a.Length
                || e.Where((t, i) => !t.IsAssignableFrom(a[i]) && !a[i].IsAssignableFrom(t)).Any())
                throw new InvalidOperationException($"[Analyzer] {wrapper.DeclaringType?.Name}.{wrapper.Name} does not match the call to {Utility.GetSignature(method)}");
        }
    }
}
