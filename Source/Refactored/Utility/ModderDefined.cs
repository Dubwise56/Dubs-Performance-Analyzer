using Analyzer;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace Analyzer
{
    public static class DynamicTypeBuilder
    {
        private static readonly TypeAttributes staticAtt = TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;
        // this is what a static class looks like in attributes

        private static AssemblyBuilder assembly = null;
        private static AssemblyBuilder Assembly => assembly ??= assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("DubsDynamicTypes"), AssemblyBuilderAccess.Run);

        private static ModuleBuilder moduleBuilder = null;
        private static ModuleBuilder ModuleBuilder => moduleBuilder ??= Assembly.DefineDynamicModule("DubsDynamicTypes", "DubsDynamicTypes.dll");

        public static Dictionary<string, HashSet<MethodInfo>> methods = new Dictionary<string, HashSet<MethodInfo>>();

        public static Type CreateType(string name, HashSet<MethodInfo> methods)
        {
            TypeBuilder tb = ModuleBuilder.DefineType(name, staticAtt, typeof(Entry));

            FieldBuilder active = tb.DefineField("Active", typeof(bool), FieldAttributes.Public | FieldAttributes.Static);

            DynamicTypeBuilder.methods.Add(name, methods);

            ConstructorBuilder ivCtor = tb.DefineConstructor(MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, new Type[0]);
            ILGenerator ctorIL = ivCtor.GetILGenerator();
            ctorIL.Emit(OpCodes.Ldc_I4_0);
            ctorIL.Emit(OpCodes.Stsfld, active);
            ctorIL.Emit(OpCodes.Ret);

            CreatePrefix(tb, active);
            CreatePostfix(tb, active);

            CreateProfilePatch(name, tb);

            return tb.CreateType();
        }

        // We already have our methods defined inside a dictionary, all we need to do is index into it at runtime
        private static void CreateProfilePatch(string name, TypeBuilder tb)
        {
            MethodInfo func = AccessTools.Method(typeof(DynamicTypeBuilder), "PatchAll");

            MethodBuilder ProfilePatch = tb.DefineMethod("ProfilePatch", MethodAttributes.Public | MethodAttributes.Static);

            ILGenerator generator = ProfilePatch.GetILGenerator();

            generator.Emit(OpCodes.Ldstr, name);
            generator.Emit(OpCodes.Call, func);
            generator.Emit(OpCodes.Ret);
        }

        public static void PatchAll(string name)
        {
            MethodInfo premeth = AccessTools.Method(name + ":Prefix");
            MethodInfo postmeth = AccessTools.Method(name + ":Postfix");

            HarmonyMethod pre = new HarmonyMethod(premeth, Priority.Last);
            HarmonyMethod post = new HarmonyMethod(postmeth, Priority.First);

            foreach (MethodInfo meth in methods[name])
                Modbase.Harmony.Patch(meth, pre, post);
        }

        private static void LogMethod(MethodInfo info)
        {
            Log.Message($"{info.Name} with the return type {info.ReturnType.Name}");

            Log.Message("Params");
            foreach (ParameterInfo param in info.GetParameters())
            {
                Log.Message($"Parameter: {param.Position} of the type: {param.ParameterType.Name} named: {param.Name}");
            }
        }


        private static void CreatePrefix(TypeBuilder tb, FieldBuilder active)
        {
            MethodInfo getDeclType = AccessTools.Method(typeof(MethodInfo), "get_DeclaringType");
            MethodInfo getName = AccessTools.Method(typeof(MethodInfo), "get_Name");
            MethodInfo format = AccessTools.Method(typeof(String), "Format", new Type[] { typeof(string), typeof(object), typeof(object) });
            MethodInfo start = AccessTools.Method(typeof(Modbase), nameof(ProfileController.Start));

            MethodBuilder prefix = tb.DefineMethod(
                "Prefix",
                MethodAttributes.Public | MethodAttributes.Static,
                null,
                new Type[] { typeof(MethodBase), typeof(object), typeof(Profiler).MakeByRefType() });


            // name our params as such as per Harmonys constants: https://github.com/pardeike/Harmony/blob/master/Harmony/Internal/MethodPatcher.cs#L13-L21
            prefix.DefineParameter(1, ParameterAttributes.None, "__originalMethod");
            prefix.DefineParameter(2, ParameterAttributes.None, "__instance");
            prefix.DefineParameter(3, ParameterAttributes.In, "__state");


            ILGenerator generator = prefix.GetILGenerator();
            Label skipLabel = generator.DefineLabel();

            // if(Active && AnalyzerState.CurrentlyRunning)
            // { 
            //...
            generator.Emit(OpCodes.Ldsfld, active);
            generator.Emit(OpCodes.Brfalse_S, skipLabel);

            generator.Emit(OpCodes.Ldsfld, AccessTools.Field(typeof(AnalyzerState), "CurrentlyRunning"));
            generator.Emit(OpCodes.Brfalse_S, skipLabel);

            generator.Emit(OpCodes.Nop);

            // call __state = ProfileController.Start($"{__originalMethod.DeclaringType} - {__originalMethod.Name}", null, null, null, null, __originalMethod as MethodInfo);
            generator.Emit(OpCodes.Ldarg_2); // __state
            generator.Emit(OpCodes.Ldstr, "{0} - {1}"); // format string
            generator.Emit(OpCodes.Ldarg_0); // declaring type
            generator.Emit(OpCodes.Callvirt, getDeclType);
            generator.Emit(OpCodes.Ldarg_0); // name
            generator.Emit(OpCodes.Callvirt, getName);
            generator.Emit(OpCodes.Call, format); // String.Format(str, obj, obj)
            generator.Emit(OpCodes.Ldnull);
            generator.Emit(OpCodes.Ldnull);
            generator.Emit(OpCodes.Ldnull);
            generator.Emit(OpCodes.Ldnull);
            generator.Emit(OpCodes.Ldarg_0); // __originalMethod
            generator.Emit(OpCodes.Call, start); // ProfileController.Start
            generator.Emit(OpCodes.Stind_Ref);

            // ...
            // }
            // return;
            generator.Emit(OpCodes.Nop);
            generator.MarkLabel(skipLabel);

            generator.Emit(OpCodes.Ret);
        }

        private static void CreatePostfix(TypeBuilder tb, FieldBuilder active)
        {
            MethodInfo end = AccessTools.Method(typeof(Profiler), "Stop");

            MethodBuilder postfix = tb.DefineMethod(
            "Postfix",
            MethodAttributes.Public | MethodAttributes.Static,
            null,
            new Type[] { typeof(Profiler) });

            postfix.DefineParameter(1, ParameterAttributes.None, "__state");

            ILGenerator generator = postfix.GetILGenerator();
            Label skipLabel = generator.DefineLabel();

            generator.Emit(OpCodes.Ldsfld, active);
            generator.Emit(OpCodes.Brfalse_S, skipLabel);

            generator.Emit(OpCodes.Ldsfld, AccessTools.Field(typeof(AnalyzerState), "CurrentlyRunning"));
            generator.Emit(OpCodes.Brfalse_S, skipLabel);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, end);

            generator.MarkLabel(skipLabel);
            generator.Emit(OpCodes.Ret);
        }

    }

}
