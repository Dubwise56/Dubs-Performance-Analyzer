using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Verse;

namespace Analyzer.Profiling
{
    public static class MethodTransplanting
    {
        public static readonly HashSet<MethodInfo> patchedMeths = [];
        public static readonly ConcurrentDictionary<MethodBase, Type> typeInfo = [];

        private static readonly HarmonyMethod transpiler = new(typeof(MethodTransplanting), nameof(Transpiler));

        // profile controller
        private static readonly MethodInfo ProfileController_Start
            = AccessTools.Method(typeof(ProfileController), nameof(ProfileController.Start));

        private static readonly FieldInfo ProfilerController_Profiles
            = AccessTools.Field(typeof(ProfileController), nameof(ProfileController.profiles));

        // profiler
        private static readonly MethodInfo
            Profiler_Start = AccessTools.Method(typeof(Profiler), nameof(Profiler.Start)),
            Profiler_Stop = AccessTools.Method(typeof(Profiler), nameof(Profiler.Stop));

        private static readonly ConstructorInfo ProfilerCtor = AccessTools.Constructor(typeof(Profiler),
            [typeof(string), typeof(string), typeof(Type), typeof(MethodBase)]);

        // analyzer
        private static readonly MethodInfo Analyzer_Get_CurrentlyProfiling
            = AccessTools.PropertyGetter(typeof(Analyzer), nameof(Analyzer.CurrentlyProfiling));
        
        private static readonly FieldInfo
            Analyzer_CurrentlyProfiling = AccessTools.Field(typeof(Analyzer), nameof(Analyzer.currentlyProfiling)),
            Analyzer_CurrentyPaused = AccessTools.Field(typeof(Analyzer), nameof(Analyzer.currentlyPaused));

        // dictionary
        private static readonly MethodInfo
            Dict_TryGetValue = AccessTools.Method(typeof(ConcurrentDictionary<string, Profiler>),
                "TryGetValue"),
            Dict_Add = AccessTools.Method(typeof(ConcurrentDictionary<string, Profiler>),
                "TryAdd");

        
        public static void ClearCaches()
        {
            patchedMeths.Clear();
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
            foreach (var meth in meths) UpdateMethod(type, meth);
        }
            
        internal static void UpdateMethod(Type type, MethodInfo meth)
        {
            if (patchedMeths.Contains(meth))
            {
#if DEBUG
                ThreadSafeLogger.Warning($"[Analyzer] Already patched method {meth.DeclaringType.FullName + ":" + meth.Name}");
#else
                if (Settings.verboseLogging)
                    ThreadSafeLogger.Warning($"[Analyzer] Already patched method {Utility.GetSignature(meth, false)}");
#endif
                return;
            }

            patchedMeths.Add(meth);
            typeInfo.TryAdd(meth, type);

            var work = () => {
                try
                {
                    Modbase.Harmony.Patch(meth, transpiler: transpiler);
                }
                catch (Exception e)
                {
#if DEBUG
                ThreadSafeLogger.ReportException(e, $"Failed to patch the method {Utility.GetSignature(meth, false)}");
#else
                    if (Settings.verboseLogging)
                        ThreadSafeLogger.ReportException(e, $"Failed to patch the method {Utility.GetSignature(meth, false)}");
#endif
                }
            };

            if (Settings.disableThreadedPatching) 
            {
                work(); 
            } else 
            {
                Task.Factory.StartNew(work);
            }
        }

        // This transpiler basically replicates ProfileController.Start, but in IL, and inside the method it is patching, to reduce as much overhead as
        // possible, its quite simple, just long and hard to read.
        public static IEnumerable<CodeInstruction> Transpiler(MethodBase __originalMethod, IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
        {
            var codes = instructions.ToList();
            var curType = typeInfo[__originalMethod];
            var curLabelMeth = curType.GetMethod("GetLabel", BindingFlags.Public | BindingFlags.Static);
            var curNamerMeth = curType.GetMethod("GetName", BindingFlags.Public | BindingFlags.Static);
            var curTypeMeth = curType.GetMethod("GetType", BindingFlags.Public | BindingFlags.Static);
            
            var key = Utility.GetMethodKey(__originalMethod); // This translates our method into a human-legible key, I.e. Namespace.Type<Generic>:Method
            var methodKey = MethodInfoCache.AddMethod(key, __originalMethod);

            var needsFullLookup = false;

            var cacheKey = key;
            if (curNamerMeth != null)
            {
                if (curNamerMeth.GetParameters() is not [_,..])
                    cacheKey = (string)curNamerMeth.Invoke(null, null);
                else
                    needsFullLookup = true;
            }

            var label = cacheKey;
            if (curLabelMeth != null && !needsFullLookup)
            {
                if (curLabelMeth.GetParameters() is not [_,..])
                    label = (string)curLabelMeth.Invoke(null, null);
                else
                    needsFullLookup = true;
            }

            Type type = null;
            if (curTypeMeth != null && !needsFullLookup)
            {
                if (curTypeMeth.GetParameters() is not [_,..])
                    type = (Type)curTypeMeth.Invoke(null, null);
                else
                    needsFullLookup = true;
            }

            if (!needsFullLookup)
            {
                foreach (var instruction in TranspilerMini(codes, ilGen, curType, cacheKey, label, type, methodKey))
                    yield  return instruction;
                
                yield break;
            }

            var profLocal = ilGen.DeclareLocal(typeof(Profiler));
            var keyLocal = ilGen.DeclareLocal(typeof(string));
            var beginLabel = ilGen.DefineLabel();
            var startProfilerLabel = ilGen.DefineLabel();
            var tryGetValueLabel = ilGen.DefineLabel();

            // Active Check
            {
                // if (CurType.Active & (Analyzer.CurrentlyProfiling & !Analyzer.CurrentlyPaused))
                yield return new CodeInstruction(OpCodes.Ldsfld, curType.GetField("Active", BindingFlags.Public | BindingFlags.Static));

                yield return new CodeInstruction(OpCodes.Ldsfld, Analyzer_CurrentlyProfiling); // profiling
                yield return new CodeInstruction(OpCodes.Ldsfld, Analyzer_CurrentyPaused); // !paused
                yield return new CodeInstruction(OpCodes.Not);
                yield return new CodeInstruction(OpCodes.And);

                yield return new CodeInstruction(OpCodes.And);
                yield return new CodeInstruction(OpCodes.Brfalse_S, beginLabel);
            }


            { // Custom Namer
                if (curNamerMeth != null)
                {
                    foreach (var codeInst in GetLoadArgsForMethodParams(__originalMethod as MethodInfo, curNamerMeth.GetParameters()))
                        yield return codeInst;

                    yield return new CodeInstruction(OpCodes.Call, curNamerMeth);
                }
                else
                {
                    yield return new CodeInstruction(OpCodes.Ldstr, key);
                }
                yield return new CodeInstruction(OpCodes.Stloc, keyLocal);
                // if our key is null, start the method (opt out by name)
                yield return new CodeInstruction(OpCodes.Ldloc, keyLocal);
                yield return new CodeInstruction(OpCodes.Brfalse, beginLabel);
            }

            { // if(Profilers.TryGetValue(key, out var prof))
                yield return new CodeInstruction(OpCodes.Ldsfld, ProfilerController_Profiles).WithLabels(tryGetValueLabel);
                yield return new CodeInstruction(OpCodes.Ldloc, keyLocal);
                yield return new CodeInstruction(OpCodes.Ldloca_S, profLocal);
                yield return new CodeInstruction(OpCodes.Callvirt, Dict_TryGetValue);
                yield return new CodeInstruction(OpCodes.Brtrue_S, startProfilerLabel);
            }

            // If we found a profiler - Start it, and skip to the start of execution of the method

            { // if not, we need to make one
                yield return new CodeInstruction(OpCodes.Ldloc, keyLocal);

                { // Custom Labelling
                    if (curLabelMeth != null)
                    {
                        foreach (var codeInst in GetLoadArgsForMethodParams(__originalMethod as MethodInfo, curLabelMeth.GetParameters()))
                            yield return codeInst;

                        yield return new CodeInstruction(OpCodes.Call, curLabelMeth);
                    }
                    else
                    {
                        yield return new CodeInstruction(OpCodes.Dup); // duplicate the key on the stack so the key is both the key and the label in ProfileController.Start
                    }
                }
                { // Custom Typing
                    if (curTypeMeth != null)
                    {
                        foreach (var codeInst in GetLoadArgsForMethodParams(__originalMethod as MethodInfo, curTypeMeth.GetParameters()))
                            yield return codeInst;

                        yield return new CodeInstruction(OpCodes.Call, curTypeMeth);
                    }
                    else
                    {
                        yield return new CodeInstruction(OpCodes.Ldnull); // duplicate the key on the stack so the key is both the key and the label in ProfileController.Start
                    }
                }

                { // get our methodinfo from the metadata
                    foreach (var inst in MethodInfoCache.GetInlineIL(methodKey))
                        yield return inst;
                }

                yield return new CodeInstruction(OpCodes.Newobj, ProfilerCtor); // new Profiler();
                yield return new CodeInstruction(OpCodes.Stloc, profLocal);
            }

            { // Add to the Profilers dictionary, so we cache creation.
                yield return new CodeInstruction(OpCodes.Ldsfld, ProfilerController_Profiles);
                yield return new CodeInstruction(OpCodes.Ldloc, keyLocal);
                yield return new CodeInstruction(OpCodes.Ldloc, profLocal);
                yield return new CodeInstruction(OpCodes.Callvirt, Dict_Add);
                yield return new(OpCodes.Brfalse_S, tryGetValueLabel);
            }

            yield return new CodeInstruction(OpCodes.Ldloc, profLocal).WithLabels(startProfilerLabel);
            yield return new CodeInstruction(OpCodes.Call, Profiler_Start);

            codes[0].WithLabels(beginLabel);

            // For each instruction which exits this function, append our finishing touches (I.e.)
            // if(profiler != null)
            // {
            //      profiler.Stop();
            // }
            // return; // any labels here are moved to the start of the `if`
            foreach (var inst in codes)
            {
                if (inst.opcode == OpCodes.Ret)
                {
                    Label endLabel = ilGen.DefineLabel();

                    // localProf?.Stop();
                    yield return new CodeInstruction(OpCodes.Ldloc, profLocal).MoveLabelsFrom(inst);
                    yield return new CodeInstruction(OpCodes.Brfalse_S, endLabel);

                    yield return new CodeInstruction(OpCodes.Ldloc, profLocal);
                    yield return new CodeInstruction(OpCodes.Call, Profiler_Stop);

                    yield return inst.WithLabels(endLabel);
                }
                else
                {
                    yield return inst;
                }
            }
        }

        private static List<CodeInstruction> TranspilerMini(List<CodeInstruction> instructions,
            ILGenerator ilGen, Type curType, string key, string label, Type type, int methodKey)
        {
            var beginLabel = ilGen.DefineLabel();

            instructions[0].WithLabels(beginLabel);
            var startIndex = 0;

            // Active Check
            {
                // if (CurType.Active & (Analyzer.CurrentlyProfiling & !Analyzer.CurrentlyPaused))
                CodeInstruction[] activeCheck =
                [
                    new(OpCodes.Ldsfld,
                        curType.GetField("Active", BindingFlags.Public | BindingFlags.Static)),
                    new(OpCodes.Ldsfld, Analyzer_CurrentlyProfiling), // profiling

                    new(OpCodes.Ldsfld, Analyzer_CurrentyPaused), // !paused
                    new(OpCodes.Not), new(OpCodes.And), new(OpCodes.And), new(OpCodes.Brfalse_S, beginLabel)
                ];

                instructions.InsertRange(0, activeCheck);
                startIndex += activeCheck.Length;
            }

            var profiler = GetOrAddPinnedProfiler(key, label, type, methodKey);

            if (profiler == null)
                return instructions;

            var getProfiler = GetProfilerThroughConstInstruction(profiler);

            var toObject = new CodeInstruction(OpCodes.Conv_I);

            CodeInstruction[] profilerStart =
            [
                getProfiler, toObject, new(OpCodes.Call, Profiler_Start)
            ];
            
            instructions.InsertRange(startIndex, profilerStart);
            startIndex += profilerStart.Length;

            // For each instruction which exits this function, append our finishing touches (I.e.)
            // profiler.Stop();
            // return; // any labels here are moved to the start of the `if`
            for (var i = instructions.Count; --i >= startIndex;)
            {
                var instruction = instructions[i];
                if (instruction.opcode == OpCodes.Ret)
                {
                    instructions.InsertRange(i,
                    [
                        getProfiler.Clone().MoveLabelsFrom(instruction), toObject.Clone(),
                        new(OpCodes.Call, Profiler_Stop)
                    ]);
                }
            }

            return instructions;
        }

        private static Profiler GetOrAddPinnedProfiler(string key, string label, Type type, int methodKey)
        {
            var profiles = ProfileController.Profiles;

            if (!profiles.TryGetValue(key, out var profiler) || profiler == null)
            {
                var handle = GCHandle.Alloc(profiler = new(key, label, type, MethodInfoCache.Get(methodKey)), GCHandleType.Pinned);
                if (!profiles.TryAdd(key, profiler))
                {
                    profiler = profiles[key];
                    handle.Free();
                }
                else
                {
                    ProfileController.Handles.Add(handle);
                }
            }

            if (profiler == null)
            {
                ThreadSafeLogger.Error($"Null profiler for type '{type}', key '{key}', label '{label}'\n{
                    new StackTrace(true)}");
            }

            return profiler;
        }

        private static unsafe CodeInstruction GetProfilerThroughConstInstruction(Profiler profiler)
        {
            var profilerPointer = *(nint*)&profiler;
            return sizeof(nint) == sizeof(long)
                ? new CodeInstruction(OpCodes.Ldc_I8, (long)profilerPointer)
                : new(OpCodes.Ldc_I4, (int)profilerPointer);
        }

        // Emulates Harmonys '__instance' & '___fieldName' & parameter sniping.
        private static List<CodeInstruction> GetLoadArgsForMethodParams(MethodInfo originalMethod, ParameterInfo[] methodparams)
        {
            var origParams = originalMethod.GetParameters();
            var origType = originalMethod.GetType();
            var insts = new List<CodeInstruction>();

            foreach (var param in methodparams)
            {
                if (param.Name == "__instance") // Trying to get the instance of the object (assumed to be non static)
                {
                    insts.Add(new CodeInstruction(OpCodes.Ldarg_0)); // Push the instance of the object on the stack (up to the user to get this right)
                }
                else if (param.Name.StartsWith("___")) // Trying to get a field from the object (static or non static)
                {
                    var fieldName = param.Name.Remove(0, 3);

                    var fieldInfo = AccessTools.Field(origType, fieldName);

                    if (fieldInfo.IsStatic)
                    {
                        insts.Add(new CodeInstruction(OpCodes.Ldsfld, fieldInfo));
                    }
                    else
                    {
                        insts.Add(new CodeInstruction(OpCodes.Ldarg_0)); // push the instance onto the stack, then grab the field from the instance.
                        insts.Add(new CodeInstruction(OpCodes.Ldfld, fieldInfo));
                    }
                }
                else // Trying to intercept a param from the original method
                {
                    insts.Add(new CodeInstruction(OpCodes.Ldarg_S, (originalMethod.IsStatic ? 0 : 1) + origParams.FirstIndexOf(p => p.Name == param.Name && p.ParameterType == param.ParameterType)));
                }
            }
            return insts;
        }


        // Utility for internal && transpiler profiling.
        // This method takes a codeinstruction (of type Call or CallVirt), a key, a type, and a fieldinfo of a dictionary
        // and will return a new codeinstruction, which the same opcode as the instruction passed to it, and the method
        // will be a new dynamic method, which duplicates the functionality of the original method, while adding proiling to it.
        // 
        // The key is used for keying into the dictionary field you give, which will be expected to return a MethodInfo
        // this will be then used in the call to ProfileController.Start
        public static CodeInstruction ReplaceMethodInstruction(CodeInstruction inst, string key, Type type, int index)
        {
            var method = inst.operand as MethodInfo;
            if (method == null)
                return inst;

            var parameterQuery = method.GetParameters().Select(param => param.ParameterType);

            var parameters
                = (method.Attributes.HasFlag(MethodAttributes.Static) // If we have a static method, we don't need to grab the instance
                    ? parameterQuery
                    : method.DeclaringType!.IsValueType // if we have a struct, we need to make the struct a ref, otherwise you resort to black magic
                        ? parameterQuery.Prepend(method.DeclaringType.MakeByRefType())
                        : parameterQuery.Prepend(method.DeclaringType)) // otherwise, we have an instance-nonstruct class, lets all our instance, and our parameter types
                .ToArray();

            var meth = new DynamicMethod(
                method.Name + "_runtimeReplacement",
                MethodAttributes.Public,
                method.CallingConvention,
                method.ReturnType,
                parameters,
                method.DeclaringType?.IsInterface ?? true ? typeof(void) : method.DeclaringType,
                true
                );

            var gen = meth.GetILGenerator(512);
            var profiler = GetOrAddPinnedProfiler(key, key, null, index);

            InsertStartIL(type, gen, key, profiler, index);

            // dynamically add our parameters, as many as they are, onto the stack for our original method
            for (var i = 0; i < parameters.Length; i++)
                gen.Emit(OpCodes.Ldarg, i);

            gen.Emit(inst.opcode, method); // call our original method, (all parameters are on the stack)

            InsertRetIL(type, gen, profiler); // wrap our function up, return a value if required

            return new(inst)
            {
                opcode = OpCodes.Call,
                operand = meth // our created dynamic method
            };
        }

        // Utility for IL insertion
        public static void InsertStartIL(Type type, ILGenerator ilGen, string key, Profiler profiler, int index)
        {
            // if (CurType.Active & (Analyzer.CurrentlyProfiling & !Analyzer.CurrentlyPaused))
            // { 
            var skipLabel = ilGen.DefineLabel();

            ilGen.Emit(OpCodes.Ldsfld, type.GetField("Active", BindingFlags.Public | BindingFlags.Static));
            ilGen.Emit(OpCodes.Ldsfld, Analyzer_CurrentlyProfiling);
            ilGen.Emit(OpCodes.Ldsfld, Analyzer_CurrentyPaused);
            ilGen.Emit(OpCodes.Not);
            ilGen.Emit(OpCodes.And);
            ilGen.Emit(OpCodes.And);
            ilGen.Emit(OpCodes.Brfalse_S, skipLabel);
            // }

            EmitGetProfilerThroughConstInstruction(ilGen, profiler);

            ilGen.Emit(OpCodes.Call, Profiler_Start);

            ilGen.MarkLabel(skipLabel);
        }

        public static void InsertRetIL(Type type, ILGenerator ilGen, Profiler profiler)
        {
            EmitGetProfilerThroughConstInstruction(ilGen, profiler);

            ilGen.Emit(OpCodes.Call, Profiler_Stop);
            ilGen.Emit(OpCodes.Ret);
        }

        private static void EmitGetProfilerThroughConstInstruction(ILGenerator ilGen, Profiler profiler)
        {
            var getProfiler = GetProfilerThroughConstInstruction(profiler);
            if (IntPtr.Size == sizeof(long))
                ilGen.Emit(getProfiler.opcode, (long)getProfiler.operand);
            else
                ilGen.Emit(getProfiler.opcode, (int)getProfiler.operand);

            ilGen.Emit(OpCodes.Conv_I);
        }
    }
}
