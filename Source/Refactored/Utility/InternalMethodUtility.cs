using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Verse;

namespace Analyzer
{
    public static class InternalMethodUtility
    {
        public static HarmonyMethod InternalProfiler = new HarmonyMethod(typeof(InternalMethodUtility), nameof(InternalMethodUtility.Transpiler));
        public static MethodInfo curMeth = null;
        public static HashSet<MethodInfo> PatchedInternals = new HashSet<MethodInfo>();


        private static Harmony inst = null;
        public static Harmony Harmony => inst ??= new Harmony("InternalMethodProfiling");

        public static Dictionary<string, MethodInfo> KeyMethods = new Dictionary<string, MethodInfo>();

        private static readonly MethodInfo AnalyzerStartMeth = AccessTools.Method(typeof(ProfileController), nameof(ProfileController.Start));
        private static readonly MethodInfo AnalyzerEndMeth = AccessTools.Method(typeof(Profiler), nameof(Profiler.Stop));

        private static readonly FieldInfo AnalyzerKeyDict = AccessTools.Field(typeof(InternalMethodUtility), "KeyMethods");
        private static readonly MethodInfo AnalyzerGetValue = AccessTools.Method(typeof(Dictionary<string, MethodInfo>), "get_Item");

        /*
         * Utility
         */
        public static bool IsFunctionCall(OpCode instruction)
        {
            return (instruction == OpCodes.Call || instruction == OpCodes.Callvirt);// || instruction == OpCodes.Calli);
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            List<CodeInstruction> instructions = new List<CodeInstruction>(codeInstructions);

            for (int i = 0; i < instructions.Count(); i++)
            {
                if (IsFunctionCall(instructions[i].opcode))
                {
                    if (i == 0 || (i != 0 && instructions[i - 1].opcode != OpCodes.Constrained)) // lets ignore complicated cases
                    {
                        CodeInstruction inst = SupplantMethodCall(instructions[i]);
                        if (inst != instructions[i])
                        {
                            instructions[i] = inst;
                        }
                    }
                }
            }

            return instructions;
        }

        /*
         * Utility for replacing method calls
         */
        public static CodeInstruction SupplantMethodCall(CodeInstruction instruction)
        {
            MethodInfo currentMethod = null;
            try
            {
                currentMethod = (MethodInfo)instruction.operand;
            }
            catch (Exception)
            {
                return instruction;
            }

            Type[] parameters = null;

            if (currentMethod.Attributes.HasFlag(MethodAttributes.Static)) // If we have a static method, we don't need to grab the instance
                parameters = currentMethod.GetParameters().Select(param => param.ParameterType).ToArray();
            else if (currentMethod.DeclaringType.IsValueType) // if we have a struct, we need to make the struct a ref, otherwise you resort to black magic
                parameters = currentMethod.GetParameters().Select(param => param.ParameterType).Prepend(currentMethod.DeclaringType.MakeByRefType()).ToArray();
            else // otherwise, we have an instance-nonstruct class, lets all our instance, and our parameter types
                parameters = currentMethod.GetParameters().Select(param => param.ParameterType).Prepend(currentMethod.DeclaringType).ToArray();


            DynamicMethod meth = new DynamicMethod(
                currentMethod.Name + "_runtimeReplacement", 
                MethodAttributes.Public, 
                currentMethod.CallingConvention,
                currentMethod.ReturnType, 
                parameters, 
                currentMethod.DeclaringType.IsInterface ? typeof(void) : currentMethod.DeclaringType, 
                true 
                );

            ILGenerator gen = meth.GetILGenerator(512);

            string key = currentMethod.DeclaringType.FullName + "." + currentMethod.Name;
            // local variable for profiler
            LocalBuilder localProfiler = gen.DeclareLocal(typeof(Profiler));

            InsertStartIL(curMeth.Name, gen, key, localProfiler);
            KeyMethods.SetOrAdd(key, currentMethod);

            // dynamically add our parameters, as many as they are, into our method
            for (int i = 0; i < parameters.Count(); i++)
                gen.Emit(OpCodes.Ldarg_S, i);

            gen.EmitCall(instruction.opcode, currentMethod, parameters); // call our original method, as per our arguments, etc.

            InsertEndIL(curMeth.Name, gen, localProfiler); // wrap our function up, return a value if required

            CodeInstruction inst = new CodeInstruction(instruction);
            inst.opcode = OpCodes.Call;
            inst.operand = meth;

            return inst;
        }

        public static void InsertStartIL(string originalMethodName, ILGenerator ilGen, string key, LocalBuilder profiler)
        {
            // if(Active && AnalyzerState.CurrentlyRunning)
            // { 
            Label skipLabel = ilGen.DefineLabel();
            InsertActiveCheck(originalMethodName, ilGen, ref skipLabel);

            ilGen.Emit(OpCodes.Ldstr, key);
            // load our string to stack

            ilGen.Emit(OpCodes.Ldnull);
            ilGen.Emit(OpCodes.Ldnull);
            ilGen.Emit(OpCodes.Ldnull);
            ilGen.Emit(OpCodes.Ldnull);
            // load our null variables

            ilGen.Emit(OpCodes.Ldsfld, AnalyzerKeyDict); // KeyMethods
            ilGen.Emit(OpCodes.Ldstr, key);
            ilGen.Emit(OpCodes.Call, AnalyzerGetValue); // KeyMethods[key]

            ilGen.Emit(OpCodes.Call, AnalyzerStartMeth);
            ilGen.Emit(OpCodes.Stloc, profiler.LocalIndex);
            // localProfiler = ProfileController.Start(key, null, null, null, null, KeyMethods[key]);

            ilGen.MarkLabel(skipLabel);
        }

        public static void InsertEndIL(string originalMethodName, ILGenerator ilGen, LocalBuilder profiler)
        {
            Label skipLabel = ilGen.DefineLabel();
            InsertActiveCheck(originalMethodName, ilGen, ref skipLabel);

            ilGen.Emit(OpCodes.Ldloc, profiler.LocalIndex);
            ilGen.Emit(OpCodes.Call, AnalyzerEndMeth);

            ilGen.MarkLabel(skipLabel);

            ilGen.Emit(OpCodes.Ret);
        }

        public static void InsertActiveCheck(string originalMethodName, ILGenerator ilGen, ref Label label)
        {
            ilGen.Emit(OpCodes.Ldsfld, AccessTools.TypeByName(originalMethodName + "-int").GetField("Active", BindingFlags.Public | BindingFlags.Static));
            ilGen.Emit(OpCodes.Brfalse_S, label);

            ilGen.Emit(OpCodes.Call, AccessTools.Method(typeof(Analyzer), "get_CurrentlyPaused"));
            ilGen.Emit(OpCodes.Brtrue_S, label);
        }
    }
}
