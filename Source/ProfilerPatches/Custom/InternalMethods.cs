using HarmonyLib;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace DubsAnalyzer
{
    public static class InternalMethods
    {

        private static Harmony inst = null;
        private static Harmony Harmony
        {
            get
            {
                if (inst == null)
                    inst = new Harmony("Dubs.InternalMethodProfiling");
                return inst;
            }
        }
        private static Dictionary<MethodInfo, List<CodeInstruction>> PatchedInternals = new Dictionary<MethodInfo, List<CodeInstruction>>();
        private static MethodInfo curMeth = null;
        
        public static void PatchMethod(MethodInfo method)
        {
            if (PatchedInternals.ContainsKey(method))
            {
                Log.Error("Trying to re-transpile an already profiled internal method");
                return;
            }
            HarmonyMethod transpiler = new HarmonyMethod(typeof(InternalMethods), nameof(Transpiler));
            curMeth = method;
            PatchedInternals.Add(method, null);
            Harmony.Patch(method, null, null, transpiler);
        }

        public static void UnpatchMethod(MethodInfo method)
        {
            HarmonyMethod untranspiler = new HarmonyMethod(typeof(InternalMethods), nameof(UnTranspiler));
            curMeth = method;
            Harmony.Patch(method, null, null, untranspiler);
            PatchedInternals.Remove(method);
        }

        private static IEnumerable<CodeInstruction> UnTranspiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            var instructions = new List<CodeInstruction>(codeInstructions);

            var oldInstructions = PatchedInternals[curMeth];

            for (int i = 0; i < instructions.Count(); i++)
            {
                if (InternalMethodUtility.IsFunctionCall(instructions[i].opcode))
                {
                    if (i != 0 || instructions[i - 1].opcode != OpCodes.Constrained)
                    {// if we have the same conditions we had when we patched it, lets unpatch it!
                        instructions[i] = oldInstructions[i];
                    }   
                }
            }

            return instructions;
        }
        
        public static void UnpatchAllMethods()
        {
            foreach(var meth in PatchedInternals.Keys)
            {
                curMeth = meth;
                UnpatchMethod(meth);
            }
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            var instructions = new List<CodeInstruction>(codeInstructions);
            var oldInstructions = new List<CodeInstruction>();

            for (int i = 0; i < instructions.Count(); i++)
            {
                if (InternalMethodUtility.IsFunctionCall(instructions[i].opcode))
                {
                    if (i != 0 || instructions[i - 1].opcode != OpCodes.Constrained) // lets ignore complicated cases
                    {
                        oldInstructions.Add(instructions[i]);
                        instructions[i] = SupplantMethodCall(instructions[i]);
                    }
                }
            }
            PatchedInternals[curMeth] = oldInstructions;

            return instructions;
        }
        private static CodeInstruction SupplantMethodCall(CodeInstruction instruction)
        {
            MethodInfo currentMethod = (MethodInfo)instruction.operand;

            Type[] parameters = null;

            if (currentMethod.Attributes.HasFlag(MethodAttributes.Static)) // If we have a static method, we don't need to grab the instance
                parameters = currentMethod.GetParameters().Select(param => param.ParameterType).ToArray();
            else if (currentMethod.DeclaringType.IsValueType) // if we have a struct, we need to make the struct a ref, otherwise you resort to black magic
                parameters = currentMethod.GetParameters().Select(param => param.ParameterType).Prepend(currentMethod.DeclaringType.MakeByRefType()).ToArray();
            else // otherwise, we have an instance-nonstruct class, lets all our instance, and our parameter types
                parameters = currentMethod.GetParameters().Select(param => param.ParameterType).Prepend(currentMethod.DeclaringType).ToArray();


            var meth = new DynamicMethod(
                currentMethod.Name + "_runtimeReplacement", // name
                MethodAttributes.Public, // attributes
                currentMethod.CallingConvention, // callingconvention
                currentMethod.ReturnType, // returntype
                parameters, // parameters
                currentMethod.DeclaringType.IsInterface ? typeof(void) : currentMethod.DeclaringType, // owner
                true // skipVisibility
                );

            ILGenerator gen = meth.GetILGenerator(512);

            string key = currentMethod.Name;

            InternalMethodUtility.InsertStartIL(gen, key);

            // dynamically add our parameters, as many as they are, into our method
            for (int i = 0; i < parameters.Count(); i++)
                gen.Emit(OpCodes.Ldarg_S, i);

            gen.EmitCall(instruction.opcode, currentMethod, parameters); // call our original method, as per our arguments, etc.

            InternalMethodUtility.InsertEndIL(gen, key); // wrap our function up, return a value if required

            var inst = new CodeInstruction(instruction);
            inst.opcode = OpCodes.Call;
            inst.operand = meth;

            return inst;
        }

    }
}
