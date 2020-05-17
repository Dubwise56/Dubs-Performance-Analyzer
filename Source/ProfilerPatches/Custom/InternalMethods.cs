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
        //public static MethodInfo GetHashCode = AccessTools.Method("System.Object:GetHashCode");
        //public static MethodInfo ToString = AccessTools.Method("System.Int32:ToString"); // We use this to convert our hashcode into a string

        public static MethodInfo AnalyzerStartMeth = AccessTools.Method(typeof(InternalMethods), nameof(AnalyzerStart));
        public static MethodInfo AnalyzerEndMeth = AccessTools.Method(typeof(InternalMethods), nameof(AnalyzerEnd));

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
        {

            List<CodeInstruction> instructions = new List<CodeInstruction>(codeInstructions);

            for (int i = 0; i < instructions.Count(); i++)
            {
                if (InternalMethodUtility.IsFunctionCall(instructions[i].opcode))
                {
                    instructions[i] = SupplantMethodCall(instructions[i]);
                }
            }

            foreach (var l in instructions)
                InternalMethodUtility.LogInstruction(l);

            return instructions;
        }

        public static CodeInstruction SupplantMethodCall(CodeInstruction instruction)
        {
            MethodInfo currentMethod = (MethodInfo)instruction.operand;

            Type[] parameters = null;

            if (currentMethod.IsStatic) // If we have a static method, we don't need to grab the instance
                parameters = currentMethod.GetParameters().Select(param => param.ParameterType).ToArray();
            else if (currentMethod.DeclaringType.IsValueType) // if we have a struct, we need to make the struct a ref, otherwise you resort to black magic
                parameters = currentMethod.GetParameters().Select(param => param.ParameterType).Prepend(currentMethod.DeclaringType.MakeByRefType()).ToArray();
            else // otherwise, we have an instance-nonstruct class, lets all our instance, and our parameter types
                parameters = currentMethod.GetParameters().Select(param => param.ParameterType).Prepend(currentMethod.DeclaringType).ToArray();


            var meth = new DynamicMethod(
                currentMethod.Name + "_runtimeReplacement", // name
                currentMethod.Attributes, // attributes
                currentMethod.CallingConvention, // callingconvention
                currentMethod.ReturnType, // returntype
                parameters, // parameters
                currentMethod.DeclaringType.IsInterface ? typeof(void) : currentMethod.DeclaringType, // owner
                true // skipVisibility
                );

            ILGenerator gen = meth.GetILGenerator(512);

            string key = currentMethod.Name.GetHashCode().ToString();

            InsertStartIL(gen, key);

            // dynamically add our parameters, as many as they are, into our method

            for (int i = 0; i < parameters.Count(); i++)
                gen.Emit(OpCodes.Ldarg_S, i);

            gen.EmitCall(instruction.opcode, currentMethod, parameters); // call our original method, as per our arguments, etc.

            
            InsertEndIL(gen, key); // wrap out function up, return a value if required

            var inst = new CodeInstruction(instruction);
            inst.operand = meth;


            return inst;
        }

        public static void InsertStartIL(ILGenerator ilGen, string key)
        {
            ilGen.Emit(OpCodes.Ldstr, key); 
            ilGen.Emit(OpCodes.Call, AnalyzerStartMeth);
        }

        public static void AnalyzerStart(string key)
        {
            Analyzer.Start(key);
        }

        public static void InsertEndIL(ILGenerator ilGen, string key)
        {
            ilGen.Emit(OpCodes.Ldstr, key);
            ilGen.Emit(OpCodes.Call, AnalyzerEndMeth);

            ilGen.Emit(OpCodes.Ret);
        }

        public static void AnalyzerEnd(string key)
        {
            Analyzer.Start(key);
        }
    }
}
