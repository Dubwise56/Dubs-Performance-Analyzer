using HarmonyLib;
using RimWorld.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Analyzer
{
    public class CodeInstMethEqual : EqualityComparer<CodeInstruction>
    {
        public override bool Equals(CodeInstruction b1, CodeInstruction b2)
        {
            if (b1.opcode != b2.opcode) return false;

            if (InternalMethodUtility.IsFunctionCall(b1.opcode))
            {
                try
                {
                    return ((MethodInfo)b1.operand).Name == ((MethodInfo)b2.operand).Name;
                }
                catch { }
            }

            return b1.operand == b2.operand;
        }

        public override int GetHashCode(CodeInstruction obj)
        {
            return obj.GetHashCode();
        }
    }

    [Entry("HarmonyTranspilers", Category.Update, "TransPatchTipKey")]
    public static class H_HarmonyTranspilers
    {
        public static bool Active = false;
        public static List<MethodBase> PatchedMeths = new List<MethodBase>();
        public static Dictionary<string, MethodInfo> MethodInfos = new Dictionary<string, MethodInfo>();
        public static CodeInstMethEqual methComparer = new CodeInstMethEqual();

        public static List<Thread> transpilerThreads = new List<Thread>();

        public static object PatchedMethsSync = new object();

        public static void ProfilePatch()
        {
            HarmonyMethod trans = new HarmonyMethod(typeof(H_HarmonyTranspilers), nameof(Transpiler));

            List<MethodBase> patches = Harmony.GetAllPatchedMethods().Where(meth => Harmony.GetPatchInfo(meth).Transpilers?.Any(p => p.owner != Modbase.Harmony.Id && !PatchedMeths.Contains(meth)) ?? false).ToList();

            foreach (MethodBase method in patches)
            {
                try
                { 
                    PatchedMeths.Add(method);
                    Modbase.Harmony.Patch(method, transpiler: trans);
                }
                catch { }
            }

        }

        [HarmonyPriority(Priority.Last)]
        public static IEnumerable<CodeInstruction> Transpiler(MethodBase __originalMethod, IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
        {
            List<CodeInstruction> inst = PatchProcessor.GetOriginalInstructions(__originalMethod);
            List<CodeInstruction> modInstList = instructions.ToList();

            Myers<CodeInstruction> insts = new Myers<CodeInstruction>(inst.ToArray(), modInstList.ToArray(), methComparer);
            insts.Compute();

            foreach (var thing in insts.changeSet)
            {
                if (thing.change != ChangeType.Added) continue;
                if (!InternalMethodUtility.IsFunctionCall(thing.value.opcode)) continue;

                var key = __originalMethod.DeclaringType.FullName + ":" + __originalMethod.Name;
                if (!MethodInfos.ContainsKey(key))
                {
                    MethodInfos.Add(key, (MethodInfo)__originalMethod);
                }

                var replaceInstruction = SupplantMethodCall(thing.value, key);

                for (int i = 0; i < modInstList.Count; i++)
                {
                    var instruction = modInstList[i];
                    if (InternalMethodUtility.IsFunctionCall(instruction.opcode))
                    {
                        if (((MethodInfo)instruction.operand).Name == ((MethodInfo)thing.value.operand).Name)
                        {
                            if (instruction != replaceInstruction)
                                modInstList[i] = replaceInstruction;
                            break;
                        }
                    }
                }
            }

            return modInstList;
        }

        private static MethodInfo ProfilerControllerStart = AccessTools.Method(typeof(ProfileController), nameof(ProfileController.Start));
        private static MethodInfo ProfilerStop = AccessTools.Method(typeof(Profiler), nameof(Profiler.Stop));

        private static FieldInfo MethodInfosDict = AccessTools.Field(typeof(H_HarmonyTranspilers), "MethodInfos");
        private static MethodInfo MethodInfosDictGet = AccessTools.Method(typeof(Dictionary<string, MethodInfo>), "get_Item");

        /*
         * Utility for replacing method calls
         */
        public static CodeInstruction SupplantMethodCall(CodeInstruction instruction, string key)
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

            // local variable for profiler
            LocalBuilder localProfiler = gen.DeclareLocal(typeof(Profiler));

            InsertStartIL(gen, key, localProfiler);

            // dynamically add our parameters, as many as they are, into our method
            for (int i = 0; i < parameters.Count(); i++)
                gen.Emit(OpCodes.Ldarg_S, i);

            gen.EmitCall(instruction.opcode, currentMethod, parameters); // call our original method, as per our arguments, etc.

            InsertEndIL(gen, localProfiler); // wrap our function up, return a value if required

            return new CodeInstruction(instruction)
            {
                opcode = OpCodes.Call,
                operand = meth
            };
        }

        public static void InsertStartIL(ILGenerator ilGen, string key, LocalBuilder profiler)
        {
            // if(H_HarmonyTranspilers.Active && !Analyzer.CurrentlyPaused)
            // { 
            Label skipLabel = ilGen.DefineLabel();
            InsertActiveCheck(ilGen, ref skipLabel);

            ilGen.Emit(OpCodes.Ldstr, key);
            // load our string to stack

            ilGen.Emit(OpCodes.Ldnull);
            ilGen.Emit(OpCodes.Ldnull);
            ilGen.Emit(OpCodes.Ldnull);
            ilGen.Emit(OpCodes.Ldnull);
            // load our null variables

            ilGen.Emit(OpCodes.Ldsfld, MethodInfosDict); // methodInfos
            ilGen.Emit(OpCodes.Ldstr, key);
            ilGen.Emit(OpCodes.Call, MethodInfosDictGet); // methodInfos[key]

            ilGen.Emit(OpCodes.Call, ProfilerControllerStart);
            ilGen.Emit(OpCodes.Stloc, profiler.LocalIndex);
            // localProfiler = ProfileController.Start(key, null, null, null, null, KeyMethods[key]);

            ilGen.MarkLabel(skipLabel);
        }

        public static void InsertEndIL(ILGenerator ilGen, LocalBuilder profiler)
        {
            Label skipLabel = ilGen.DefineLabel();
            InsertActiveCheck(ilGen, ref skipLabel);

            ilGen.Emit(OpCodes.Ldloc, profiler.LocalIndex);
            ilGen.Emit(OpCodes.Call, ProfilerStop);
            ilGen.MarkLabel(skipLabel);

            ilGen.Emit(OpCodes.Ret);
        }

        public static void InsertActiveCheck(ILGenerator ilGen, ref Label label)
        {
            ilGen.Emit(OpCodes.Ldsfld, AccessTools.Field(typeof(H_HarmonyTranspilers), nameof(H_HarmonyTranspilers.Active)));
            ilGen.Emit(OpCodes.Brfalse_S, label);

            ilGen.Emit(OpCodes.Call, AccessTools.Method(typeof(Analyzer), "get_CurrentlyPaused"));
            ilGen.Emit(OpCodes.Brtrue_S, label);
        }
    }
}




