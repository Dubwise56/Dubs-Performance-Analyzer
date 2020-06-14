using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace DubsAnalyzer
{
    public static class DynamicTypeBuilder
    {

        static AssemblyBuilder assembly = null;
        static TypeAttributes staticAtt = TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;
        // this is what a static class looks like in attributes

        static AssemblyBuilder Assembly
        {
            get
            {
                if (assembly == null)
                {
                    assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("DubsDynamicTypes"), AssemblyBuilderAccess.RunAndSave, Directory.GetCurrentDirectory() + "/Modded");
                }
                return assembly;
            }
        }
        static ModuleBuilder moduleBuilder = null;
        static ModuleBuilder ModuleBuilder
        {
            get
            {
                if (moduleBuilder == null)
                {
                    moduleBuilder = Assembly.DefineDynamicModule("DubsDynamicTypes", "DubsDynamicTypes.dll");
                }
                return moduleBuilder;
            }
        }

        public static Dictionary<string, HashSet<MethodInfo>> methods = new Dictionary<string, HashSet<MethodInfo>>();

        public static Type CreateType(string name, HashSet<MethodInfo> methods)
        {
            TypeBuilder tb = ModuleBuilder.DefineType(name, staticAtt, typeof(ProfileMode));

            var active = tb.DefineField("Active", typeof(bool), FieldAttributes.Public | FieldAttributes.Static);

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
            var func = AccessTools.Method(typeof(DynamicTypeBuilder), "PatchAll");

            MethodBuilder ProfilePatch = tb.DefineMethod("ProfilePatch", MethodAttributes.Public | MethodAttributes.Static);

            var generator = ProfilePatch.GetILGenerator();

            generator.Emit(OpCodes.Ldstr, name);
            generator.Emit(OpCodes.Call, func);
            generator.Emit(OpCodes.Ret);
        }

        public static void PatchAll(string name)
        {
            var premeth = AccessTools.Method(name + ":Prefix");
            var postmeth = AccessTools.Method(name + ":Postfix");

            HarmonyMethod pre = new HarmonyMethod(premeth, Priority.Last);
            HarmonyMethod post = new HarmonyMethod(postmeth, Priority.First);

            foreach (var meth in methods[name])
                Analyzer.harmony.Patch(meth, pre, post);
        }

        private static void LogMethod(MethodInfo info)
        {
            Log.Message($"{info.Name} with the return type {info.ReturnType.Name}");

            Log.Message("Params");
            foreach(var param in info.GetParameters())
            {
                Log.Message($"Parameter: {param.Position} of the type: {param.ParameterType.Name} named: {param.Name}");
            }
        }


        private static void CreatePrefix(TypeBuilder tb, FieldBuilder active)
        {
            var getDeclType = AccessTools.Method(typeof(MethodInfo), "get_DeclaringType");
            var getName = AccessTools.Method(typeof(MethodInfo), "get_Name");
            var format = AccessTools.Method(typeof(String), "Format", new Type[] { typeof(string), typeof(object), typeof(object) });
            var start = AccessTools.Method(typeof(Analyzer), nameof(Analyzer.Start));

            MethodBuilder prefix = tb.DefineMethod(
                "Prefix",
                MethodAttributes.Public | MethodAttributes.Static,
                null,
                new Type[] { typeof(MethodBase), typeof(object), typeof(Profiler).MakeByRefType() });


            // name our params as such as per Harmonys constants: https://github.com/pardeike/Harmony/blob/master/Harmony/Internal/MethodPatcher.cs#L13-L21
            prefix.DefineParameter(1, ParameterAttributes.None, "__originalMethod");
            prefix.DefineParameter(2, ParameterAttributes.None, "__instance");
            prefix.DefineParameter(3, ParameterAttributes.In, "__state");


            var generator = prefix.GetILGenerator();
            var skipLabel = generator.DefineLabel();

            // if(Active && AnalyzerState.CurrentlyRunning)
            // { 
            //...
            generator.Emit(OpCodes.Ldsfld, active);
            generator.Emit(OpCodes.Brfalse_S, skipLabel);

            generator.Emit(OpCodes.Ldsfld, AccessTools.Field(typeof(AnalyzerState), "CurrentlyRunning"));
            generator.Emit(OpCodes.Brfalse_S, skipLabel);

            generator.Emit(OpCodes.Nop);

            // call __state = Analyzer.Start($"{__originalMethod.DeclaringType} - {__originalMethod.Name}", null, null, null, null, __originalMethod as MethodInfo);
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
            generator.Emit(OpCodes.Call, start); // Analyzer.Start
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
            var end = AccessTools.Method(typeof(Profiler), "Stop");

            MethodBuilder postfix = tb.DefineMethod(
            "Postfix",
            MethodAttributes.Public | MethodAttributes.Static,
            null,
            new Type[] { typeof(string) });

            postfix.DefineParameter(1, ParameterAttributes.None, "__state");

            var generator = postfix.GetILGenerator();
            var skipLabel = generator.DefineLabel();

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
