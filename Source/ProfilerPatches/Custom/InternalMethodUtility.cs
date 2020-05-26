using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace DubsAnalyzer
{
    public static class InternalMethodUtility
    {
        public static HarmonyMethod InternalProfiler = new HarmonyMethod(typeof(InternalMethodUtility), nameof(InternalMethodUtility.Transpiler));
        public static HarmonyMethod UnProfiler = new HarmonyMethod(typeof(InternalMethodUtility), nameof(InternalMethodUtility.UnTranspiler));
        public static MethodInfo curMeth = null;
        public static Dictionary<MethodInfo, List<CodeInstruction>> PatchedInternals = new Dictionary<MethodInfo, List<CodeInstruction>>();


        private static Harmony inst = null;
        public static Harmony Harmony
        {
            get
            {
                if (inst == null)
                    inst = new Harmony("Dubs.InternalMethodProfiling");
                return inst;
            }
        }

        public static Dictionary<string, MethodInfo> KeyMethods = new Dictionary<string, MethodInfo>();

        private static MethodInfo AnalyzerStartMeth = AccessTools.Method(typeof(InternalMethodUtility), nameof(AnalyzerStart));
        private static MethodInfo AnalyzerEndMeth = AccessTools.Method(typeof(InternalMethodUtility), nameof(AnalyzerEnd));

        private static FieldInfo AnalyzerKeyDict = AccessTools.Field(typeof(InternalMethodUtility), "KeyMethods");
        private static MethodInfo AnalyzerGetValue = AccessTools.Method(typeof(Dictionary<string, MethodInfo>), "get_Item");

        /*
         * Utility
         */
        public static bool IsFunctionCall(OpCode instruction)
        {
            return (instruction == OpCodes.Call || instruction == OpCodes.Callvirt);// || instruction == OpCodes.Calli);
        }
        public static void LogInstruction(CodeInstruction instruction)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append($"Instruction Opcode: {instruction.opcode}");
            if (IsFunctionCall(instruction.opcode))
            {
                MethodInfo m = instruction.operand as MethodInfo;
                builder.Append($" function: {m.Name}, with the return type of: {m.ReturnType.Name}");
                if (m.GetParameters().Count() != 0)
                {
                    builder.Append(" With the parameters;");
                    foreach (var p in m.GetParameters())
                    {
                        builder.Append($" Type: {p.ParameterType.ToString()}, ");
                    }
                }
                else
                {
                    builder.Append(" With no parameters");
                }
            }
            if (instruction.labels?.Count != 0)
            {
                foreach (var l in instruction.labels)
                    builder.Append($" with the label: {l.ToString()}");
            }
            Log.Message(builder.ToString());
        }

        /*
         * Internal transpiling a method
         */
        private static IEnumerable<CodeInstruction> UnTranspiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            var oldInstructions = PatchedInternals[curMeth];

            return oldInstructions;
        }
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            var instructions = new List<CodeInstruction>(codeInstructions);
            var oldInstructions = new List<CodeInstruction>(codeInstructions);

            for (int i = 0; i < instructions.Count(); i++)
            {
                if (IsFunctionCall(instructions[i].opcode))
                {
                    if (i != 0 || (i != 0 && instructions[i - 1].opcode != OpCodes.Constrained)) // lets ignore complicated cases
                    {
                        var inst = SupplantMethodCall(instructions[i]);
                        if (inst != instructions[i])
                        {
                            instructions[i] = inst;
                        }
                    }
                }
            }
            PatchedInternals[curMeth] = oldInstructions;

            return instructions;
        }
        
        /*
         * Utility for replacing method calls
         */
        private static CodeInstruction SupplantMethodCall(CodeInstruction instruction)
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

            string key = currentMethod.DeclaringType.FullName + "." + currentMethod.Name;

            InsertStartIL(gen, key);
            KeyMethods.SetOrAdd(key, currentMethod);

            // dynamically add our parameters, as many as they are, into our method
            for (int i = 0; i < parameters.Count(); i++)
                gen.Emit(OpCodes.Ldarg_S, i);

            gen.EmitCall(instruction.opcode, currentMethod, parameters); // call our original method, as per our arguments, etc.

            InsertEndIL(gen, key); // wrap our function up, return a value if required

            var inst = new CodeInstruction(instruction);
            inst.opcode = OpCodes.Call;
            inst.operand = meth;

            return inst;
        }

        public static void InsertStartIL(ILGenerator ilGen, string key)
        {
            ilGen.Emit(OpCodes.Ldstr, key);
            // load our string to stack

            ilGen.Emit(OpCodes.Ldsfld, AnalyzerKeyDict); // KeyMethods
            ilGen.Emit(OpCodes.Ldstr, key);
            ilGen.Emit(OpCodes.Call, AnalyzerGetValue); // KeyMethods.get_Item(key) or KeyMethods[key]
                                                        // KeyMethods[key]

            ilGen.Emit(OpCodes.Call, AnalyzerStartMeth);
            // AnalyzerStart(key, KeyMethods[key]);
        }
        public static void AnalyzerStart(string key, MethodInfo meth)
        {
            Analyzer.Start(key, null, null, null, null, meth);
        }

        public static void InsertEndIL(ILGenerator ilGen, string key)
        {
            ilGen.Emit(OpCodes.Ldstr, key);
            ilGen.Emit(OpCodes.Call, AnalyzerEndMeth);

            ilGen.Emit(OpCodes.Ret);
        }
        private static void AnalyzerEnd(string key)
        {
            Analyzer.Stop(key);
        }
    }
}
