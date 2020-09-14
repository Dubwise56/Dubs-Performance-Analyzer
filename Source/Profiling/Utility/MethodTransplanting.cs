using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Profiling
{
    public static class MethodTransplanting
    {
        public static Dictionary<string, MethodInfo> methods = new Dictionary<string, MethodInfo>();

        private static readonly MethodInfo AnalyzerStartMeth = AccessTools.Method(typeof(ProfileController), nameof(ProfileController.Start));
        private static readonly MethodInfo AnalyzerEndMeth = AccessTools.Method(typeof(Profiler), nameof(Profiler.Stop));
        private static readonly MethodInfo AnalyzerPausedMeth = AccessTools.Method(typeof(Analyzer), "get_CurrentlyPaused");

        private static readonly FieldInfo MethodDictionary = AccessTools.Field(typeof(MethodTransplanting), "methods");
        private static readonly MethodInfo AnalyzerGetValue = AccessTools.Method(typeof(Dictionary<string, MethodInfo>), "get_Item");


        public static CodeInstruction ReplaceMethodInstruction(CodeInstruction inst, Type type)
        {
            MethodInfo method = null;
            try
            {
                method = (MethodInfo)inst.operand;
            }
            catch (Exception) { return inst; }

            Type[] parameters = null;

            if (method.Attributes.HasFlag(MethodAttributes.Static)) // If we have a static method, we don't need to grab the instance
                parameters = method.GetParameters().Select(param => param.ParameterType).ToArray();
            else if (method.DeclaringType.IsValueType) // if we have a struct, we need to make the struct a ref, otherwise you resort to black magic
                parameters = method.GetParameters().Select(param => param.ParameterType).Prepend(method.DeclaringType.MakeByRefType()).ToArray();
            else // otherwise, we have an instance-nonstruct class, lets all our instance, and our parameter types
                parameters = method.GetParameters().Select(param => param.ParameterType).Prepend(method.DeclaringType).ToArray();


            DynamicMethod meth = new DynamicMethod(
                method.Name + "_runtimeReplacement",
                MethodAttributes.Public,
                method.CallingConvention,
                method.ReturnType,
                parameters,
                method.DeclaringType.IsInterface ? typeof(void) : method.DeclaringType,
                true
                );

            ILGenerator gen = meth.GetILGenerator(512);

            string key = method.DeclaringType.FullName + "." + method.Name;
            // local variable for profiler
            LocalBuilder localProfiler = gen.DeclareLocal(typeof(Profiler));

            InsertStartIL(type, gen, key, localProfiler);

            if (!methods.ContainsKey(key))
                methods.Add(key, method);

            // dynamically add our parameters, as many as they are, into our method
            for (int i = 0; i < parameters.Count(); i++)
                gen.Emit(OpCodes.Ldarg_S, i);

            gen.EmitCall(inst.opcode, method, parameters); // call our original method, as per our arguments, etc.

            InsertRetIL(type, gen, localProfiler); // wrap our function up, return a value if required

            return new CodeInstruction(inst)
            {
                opcode = OpCodes.Call,
                operand = meth
            };
    }


        // Utility for IL insertion
        public static void InsertStartIL(Type type, ILGenerator ilGen, string key, LocalBuilder profiler)
        {
            // if(Active && AnalyzerState.CurrentlyRunning)
            // { 
            Label skipLabel = ilGen.DefineLabel();
            InsertActiveCheck(type, ilGen, ref skipLabel);

            ilGen.Emit(OpCodes.Ldstr, key);
            // load our string to stack

            ilGen.Emit(OpCodes.Ldnull);
            ilGen.Emit(OpCodes.Ldnull);
            ilGen.Emit(OpCodes.Ldnull);
            ilGen.Emit(OpCodes.Ldnull);
            // load our null variables

            ilGen.Emit(OpCodes.Ldsfld, MethodDictionary); // KeyMethods
            ilGen.Emit(OpCodes.Ldstr, key);
            ilGen.Emit(OpCodes.Call, AnalyzerGetValue); // KeyMethods[key]

            ilGen.Emit(OpCodes.Call, AnalyzerStartMeth);
            ilGen.Emit(OpCodes.Stloc, profiler.LocalIndex);
            // localProfiler = ProfileController.Start(key, null, null, null, null, KeyMethods[key]);

            ilGen.MarkLabel(skipLabel);
        }

        public static void InsertRetIL(Type type, ILGenerator ilGen, LocalBuilder profiler)
        {
            Label skipLabel = ilGen.DefineLabel();
            InsertActiveCheck(type, ilGen, ref skipLabel);

            ilGen.Emit(OpCodes.Ldloc, profiler.LocalIndex);
            ilGen.Emit(OpCodes.Call, AnalyzerEndMeth);

            ilGen.MarkLabel(skipLabel);

            ilGen.Emit(OpCodes.Ret);
        }

        public static void InsertActiveCheck(Type type, ILGenerator ilGen, ref Label label)
        {
            ilGen.Emit(OpCodes.Ldsfld, type.GetField("Active", BindingFlags.Public | BindingFlags.Static));
            ilGen.Emit(OpCodes.Brfalse_S, label);

            ilGen.Emit(OpCodes.Call, AnalyzerPausedMeth);
            ilGen.Emit(OpCodes.Brtrue_S, label);
        }
    }
}
