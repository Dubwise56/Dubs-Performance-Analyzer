using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace DubsAnalyzer
{
    /*
     * Requires Revision. Todo
     */
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
                    assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("DubsDynamicTypes"), AssemblyBuilderAccess.Run);
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
                    moduleBuilder = Assembly.DefineDynamicModule("MainModule");
                }
                return moduleBuilder;
            }
        }

        public static Dictionary<string, List<MethodInfo>> methods = new Dictionary<string, List<MethodInfo>>();

        public static Type CreateType(string name, UpdateMode updateMode, List<MethodInfo> methods)
        {
            TypeBuilder tb = ModuleBuilder.DefineType(name, staticAtt, typeof(ProfileMode));

            tb.DefineField("Active", typeof(bool), FieldAttributes.Public | FieldAttributes.Static);

            DynamicTypeBuilder.methods.Add(name, methods);

            CreatePrefix(tb);
            CreatePostfix(tb);

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
            StackTrace st = new StackTrace();
            Type type = st.GetFrame(1).GetMethod().DeclaringType;

            HarmonyMethod pre = null;
            HarmonyMethod post = null;
            foreach (var meth in type.GetMethods())
            {
                if (meth.Name == "Postfix")
                {
                    Log.Message("hi postfix");
                    post = new HarmonyMethod(meth);
                }
                else if (meth.Name == "Prefix")
                {
                    Log.Message("hi prefix");
                    pre = new HarmonyMethod(meth);
                }
            }

            Log.Message(name + " " + pre.ToString());

            foreach (var meth in methods[name])
            {
                Log.Message($"Trying to patch {meth.Name} using {pre.methodName} and {post.methodName} from the type {pre.declaringType.Name}");
                Analyzer.harmony.Patch(meth, pre, post);
            }
        }

        private static void CreatePrefix(TypeBuilder tb)
        {
            var getDeclType = AccessTools.Method(typeof(MethodInfo), "get_DeclaringType");
            var getName = AccessTools.Method(typeof(MethodInfo), "get_Name");
            var format = AccessTools.Method(typeof(String), "Format", new Type[] { typeof(string), typeof(object), typeof(object) });
            var start = AccessTools.Method(typeof(Analyzer), "Start");

            MethodBuilder prefix = tb.DefineMethod(
                "Prefix",
                MethodAttributes.Public | MethodAttributes.Static,
                null,
                new Type[] { typeof(MethodBase), typeof(object), typeof(string).MakeByRefType() });


            // name our params as such as per Harmonys constants: https://github.com/pardeike/Harmony/blob/master/Harmony/Internal/MethodPatcher.cs#L13-L21
            prefix.DefineParameter(0, ParameterAttributes.In, "__originalMethod");
            prefix.DefineParameter(1, ParameterAttributes.In, "__instance");
            prefix.DefineParameter(2, ParameterAttributes.In, "__state");


            var generator = prefix.GetILGenerator();
            var skipLabel = generator.DefineLabel();

            // if(Active && AnalyzerState.CurrentlyRunning)
            // { 
            // ...
            generator.Emit(OpCodes.Ldsfld, AccessTools.Field(prefix.DeclaringType, "Active"));
            generator.Emit(OpCodes.Brfalse_S, skipLabel);

            generator.Emit(OpCodes.Ldsfld, AccessTools.Field(typeof(AnalyzerState), "CurrentlyRunning"));
            generator.Emit(OpCodes.Brfalse_S, skipLabel);

            // __state = $"{__originalMethod.DeclaringType} - {__originalMethod.Name}"
            generator.Emit(OpCodes.Nop);
            generator.Emit(OpCodes.Ldarg_2); // __state
            generator.Emit(OpCodes.Ldstr, "{0} - {1}"); // format string
            generator.Emit(OpCodes.Ldarg_0); // declaring type
            generator.Emit(OpCodes.Callvirt, getDeclType);
            generator.Emit(OpCodes.Ldarg_0); // name
            generator.Emit(OpCodes.Callvirt, getName);
            generator.Emit(OpCodes.Call, format); // String.Format(str, obj, obj)
            generator.Emit(OpCodes.Stind_Ref); // store in the memory loc of our address (__state)

            // call Analyzer.Start(__state, null, null, null, null, __originalMethod as MethodInfo);
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Ldind_Ref); // label (__state)
            generator.Emit(OpCodes.Ldnull); // label func
            generator.Emit(OpCodes.Ldnull); // type?
            generator.Emit(OpCodes.Ldnull); // def?
            generator.Emit(OpCodes.Ldnull); // thing?
            generator.Emit(OpCodes.Ldarg_0); // __originalMethod
            generator.Emit(OpCodes.Isinst, typeof(MethodInfo)); // __originalMethod as MethodInfo
            generator.Emit(OpCodes.Call, start); // Analyzer.Start

            // ...
            // }
            // return;
            generator.Emit(OpCodes.Nop);
            generator.MarkLabel(skipLabel);

            generator.Emit(OpCodes.Ret);
        }

        private static void CreatePostfix(TypeBuilder tb)
        {
            var end = AccessTools.Method(typeof(Analyzer), "Stop");

            MethodBuilder postfix = tb.DefineMethod(
            "Postfix",
            MethodAttributes.Public | MethodAttributes.Static,
            null,
            new Type[] { typeof(string).MakeByRefType() });

            postfix.DefineParameter(0, ParameterAttributes.In, "__state");

            var generator = postfix.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, end);
            generator.Emit(OpCodes.Ret);
        }

    }

}
