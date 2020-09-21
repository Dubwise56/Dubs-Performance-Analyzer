using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.TestTools;
using Verse;

namespace Analyzer.Profiling
{
    public static class MethodTransplanting
    {
        public static HashSet<MethodInfo> patchedMeths = new HashSet<MethodInfo>();
        public static Dictionary<string, MethodInfo> methods = new Dictionary<string, MethodInfo>();
        public static ConcurrentDictionary<MethodBase, Type> typeInfo = new ConcurrentDictionary<MethodBase, Type>();

        // profiler
        private static readonly MethodInfo ProfilerStart = AccessTools.Method(typeof(Profiler), nameof(Profiler.Start));
        private static readonly MethodInfo ProfilerEnd = AccessTools.Method(typeof(Profiler), nameof(Profiler.Stop));
        private static readonly ConstructorInfo ProfilerCtor = AccessTools.Constructor(typeof(Profiler), new Type[] { typeof(string), typeof(string), typeof(Type), typeof(Def), typeof(Thing), typeof(MethodBase) });

        // analyzer
        private static readonly MethodInfo Analyzer_CurrentlyPaused = AccessTools.Method(typeof(Analyzer), "get_CurrentlyPaused");
        private static readonly MethodInfo Analyzer_CurrentlyProfiling = AccessTools.Method(typeof(Analyzer), "get_CurrentlyProfiling");

        // dictionary
        private static readonly MethodInfo Dict_Get_Value = AccessTools.Method(typeof(Dictionary<string, MethodInfo>), "get_Item");
        private static readonly MethodInfo Dict_TryGetValue = AccessTools.Method(typeof(Dictionary<string, Profiler>), "TryGetValue");
        private static readonly MethodInfo Dict_Add = AccessTools.Method(typeof(Dictionary<string, Profiler>), "Add");

        // dictionary fields
        private static readonly FieldInfo MethodTransplanting_Methods = AccessTools.Field(typeof(MethodTransplanting), "methods");
        private static readonly FieldInfo ProfilerController_Profiles = AccessTools.Field(typeof(ProfileController), "profiles");


        private static readonly HarmonyMethod transpiler = new HarmonyMethod(typeof(MethodTransplanting), nameof(MethodTransplanting.Transpiler));
        private static readonly MethodInfo AnalyzerStartMeth = AccessTools.Method(typeof(ProfileController), nameof(ProfileController.Start));

        public static void ClearCaches()
        {
            methods.Clear();
            typeInfo.Clear();
        }

        public static void PatchMethods(Type type)
        {
            // get the methods
            var meths = (IEnumerable<MethodInfo>)type.GetMethod("GetPatchMethods", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);

            if (meths != null)
                UpdateMethods(type, meths);
        }

        public static void UpdateMethods(Type type, IEnumerable<MethodInfo> meths)
        {
            List<Task> tasks = new List<Task>();

            foreach (var meth in meths)
            {
                if (patchedMeths.Contains(meth)) continue;

                patchedMeths.Add(meth);
                typeInfo.TryAdd(meth, type);
                try
                {
                    tasks.Add(Task.Factory.StartNew(() => Modbase.Harmony.Patch(meth, transpiler: transpiler)));
                }
                catch { }
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(MethodBase __originalMethod, IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
        {
            var profLocal = ilGen.DeclareLocal(typeof(Profiler));
            var beginLabel = ilGen.DefineLabel();
            var keyLocal = ilGen.DeclareLocal(typeof(string));
            var noProfLabel = ilGen.DefineLabel();

            var curType = typeInfo[__originalMethod];
            var curLabelMeth = curType.GetMethod("GetLabel", BindingFlags.Public | BindingFlags.Static);
            var curNamerMeth = curType.GetMethod("GetName", BindingFlags.Public | BindingFlags.Static);


            var labelIndices = new List<int>();
            var namerIndices = new List<int>();
            var paramNames = __originalMethod.GetParameters().ToArray();

            string key;
            if (__originalMethod.ReflectedType != null) key = __originalMethod.ReflectedType.FullName + ":" + __originalMethod.Name;
            else key = __originalMethod.DeclaringType.FullName + ":" + __originalMethod.Name;

            if (!methods.ContainsKey(key))
                methods.Add(key, __originalMethod as MethodInfo);


            if (curLabelMeth != null && curLabelMeth.GetParameters().Count() != 0)
            {
                foreach (var param in curLabelMeth.GetParameters())
                {
                    if (param.Name == "__instance") labelIndices.Add(0);
                    else labelIndices.Add(paramNames.FirstIndexOf(p => p.Name == param.Name && p.ParameterType == param.ParameterType));
                }
            }

            if (curNamerMeth != null && curNamerMeth.GetParameters().Count() != 0)
            {
                foreach (var param in curNamerMeth.GetParameters())
                {
                    if (param.Name == "__instance") namerIndices.Add(0);
                    else namerIndices.Add(paramNames.FirstIndexOf(p => p.Name == param.Name && p.ParameterType == param.ParameterType));
                }
            }

            // Active Check
            {
                // if(active)
                yield return new CodeInstruction(OpCodes.Ldsfld, curType.GetField("Active", BindingFlags.Public | BindingFlags.Static));
                yield return new CodeInstruction(OpCodes.Brfalse_S, beginLabel);

                // if(!Analyzer.CurrentlyPaused)
                yield return new CodeInstruction(OpCodes.Call, Analyzer_CurrentlyProfiling);
                yield return new CodeInstruction(OpCodes.Brfalse_S, beginLabel);
            }


            { // Custom Namer
                if (curNamerMeth != null)
                {
                    foreach (var index in namerIndices) yield return new CodeInstruction(OpCodes.Ldarg, index);
                    yield return new CodeInstruction(OpCodes.Call, curNamerMeth);
                }
                else
                {
                    yield return new CodeInstruction(OpCodes.Ldstr, key);
                }
                yield return new CodeInstruction(OpCodes.Stloc, keyLocal);
            }

            { // if(Profilers.TryGetValue(key, out var prof))
                yield return new CodeInstruction(OpCodes.Ldsfld, ProfilerController_Profiles);
                yield return new CodeInstruction(OpCodes.Ldloc, keyLocal);
                yield return new CodeInstruction(OpCodes.Ldloca_S, profLocal);
                yield return new CodeInstruction(OpCodes.Callvirt, Dict_TryGetValue);
                yield return new CodeInstruction(OpCodes.Brfalse_S, noProfLabel);
            }

            { // If we found a profiler - Start it, and skip to the start of execution of the method
                yield return new CodeInstruction(OpCodes.Ldloc, profLocal);
                yield return new CodeInstruction(OpCodes.Call, ProfilerStart);
                yield return new CodeInstruction(OpCodes.Pop);
                yield return new CodeInstruction(OpCodes.Br, beginLabel);
            }

            { // if not, we need to make one
                yield return new CodeInstruction(OpCodes.Ldloc, keyLocal).WithLabels(noProfLabel);

                { // Custom Labelling
                    if (curLabelMeth != null)
                    {
                        foreach (var index in labelIndices) yield return new CodeInstruction(OpCodes.Ldarg, index);
                        yield return new CodeInstruction(OpCodes.Call, curLabelMeth);
                    }
                    else
                    {
                        yield return new CodeInstruction(OpCodes.Dup); // duplicate the key on the stack so the key is both the key and the label in ProfileController.Start
                    }
                }

                yield return new CodeInstruction(OpCodes.Ldnull);
                yield return new CodeInstruction(OpCodes.Ldnull);
                yield return new CodeInstruction(OpCodes.Ldnull);

                { // get our methodinfo from the dict
                    yield return new CodeInstruction(OpCodes.Ldsfld, MethodTransplanting_Methods);
                    yield return new CodeInstruction(OpCodes.Ldstr, __originalMethod.DeclaringType.FullName + ":" + __originalMethod.Name); // idc about custom names here
                    yield return new CodeInstruction(OpCodes.Callvirt, Dict_Get_Value);
                }

                yield return new CodeInstruction(OpCodes.Newobj, ProfilerCtor); // ProfileController.Start();
                yield return new CodeInstruction(OpCodes.Dup);
                yield return new CodeInstruction(OpCodes.Stloc, profLocal);
            }

            yield return new CodeInstruction(OpCodes.Call, ProfilerStart);
            yield return new CodeInstruction(OpCodes.Pop);

            { // Add to the Profilers dictionary, so we cache creation.
                yield return new CodeInstruction(OpCodes.Ldsfld, ProfilerController_Profiles);
                yield return new CodeInstruction(OpCodes.Ldloc, keyLocal);
                yield return new CodeInstruction(OpCodes.Ldloc, profLocal);
                yield return new CodeInstruction(OpCodes.Callvirt, Dict_Add);
            }

            instructions.ElementAt(0).WithLabels(beginLabel);

            // For each instruction which exits this function, append our finishing touches
            foreach (var inst in instructions)
            {
                if (inst.opcode == OpCodes.Ret)
                {
                    Label endLabel = ilGen.DefineLabel();

                    var ldloc = new CodeInstruction(OpCodes.Ldloc, profLocal);
                    inst.MoveLabelsTo(ldloc);

                    // localProf?.Stop();
                    yield return ldloc;
                    yield return new CodeInstruction(OpCodes.Brfalse_S, endLabel);

                    yield return new CodeInstruction(OpCodes.Ldloc, profLocal);
                    yield return new CodeInstruction(OpCodes.Call, ProfilerEnd);

                    yield return inst.WithLabels(endLabel);
                }
                else
                {
                    yield return inst;
                }
            }
        }

        // Utility for internal && transpiler profiling.

        public static CodeInstruction ReplaceMethodInstruction(CodeInstruction inst, string key, Type type, FieldInfo dictFieldInfo)
        {
            MethodInfo method = null;
            try { method = (MethodInfo)inst.operand; } catch (Exception) { return inst; }

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

            // local variable for profiler
            LocalBuilder localProfiler = gen.DeclareLocal(typeof(Profiler));

            InsertStartIL(type, gen, key, localProfiler, dictFieldInfo);

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
        public static void InsertStartIL(Type type, ILGenerator ilGen, string key, LocalBuilder profiler, FieldInfo dict)
        {
            // if(Active && AnalyzerState.CurrentlyRunning)
            // { 
            Label skipLabel = ilGen.DefineLabel();

            ilGen.Emit(OpCodes.Ldsfld, type.GetField("Active", BindingFlags.Public | BindingFlags.Static));
            ilGen.Emit(OpCodes.Brfalse_S, skipLabel);

            ilGen.Emit(OpCodes.Call, Analyzer_CurrentlyProfiling);
            ilGen.Emit(OpCodes.Brfalse_S, skipLabel);

            ilGen.Emit(OpCodes.Ldstr, key);
            // load our string to stack

            ilGen.Emit(OpCodes.Ldnull);
            ilGen.Emit(OpCodes.Ldnull);
            ilGen.Emit(OpCodes.Ldnull);
            ilGen.Emit(OpCodes.Ldnull);
            // load our null variables

            ilGen.Emit(OpCodes.Ldsfld, dict); // KeyMethods
            ilGen.Emit(OpCodes.Ldstr, key);
            ilGen.Emit(OpCodes.Call, Dict_Get_Value); // KeyMethods[key]

            ilGen.Emit(OpCodes.Call, AnalyzerStartMeth);
            ilGen.Emit(OpCodes.Stloc, profiler.LocalIndex);
            // localProfiler = ProfileController.Start(key, null, null, null, null, KeyMethods[key]);

            ilGen.MarkLabel(skipLabel);
        }

        public static void InsertRetIL(Type type, ILGenerator ilGen, LocalBuilder profiler)
        {
            Label skipLabel = ilGen.DefineLabel();
            ilGen.Emit(OpCodes.Ldloc, profiler);
            ilGen.Emit(OpCodes.Brfalse_S, skipLabel);

            ilGen.Emit(OpCodes.Ldloc, profiler.LocalIndex);
            ilGen.Emit(OpCodes.Call, ProfilerEnd);

            ilGen.MarkLabel(skipLabel);

            ilGen.Emit(OpCodes.Ret);
        }
    }
}
