using Analyzer;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace Analyzer.Profiling
{
    public static class DynamicTypeBuilder
    {
        private static readonly TypeAttributes staticAtt = TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;
        // this is what a static class looks like in attributes

        private static AssemblyBuilder assembly = null;
        private static AssemblyBuilder Assembly => assembly ??= AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("DubsDynamicTypes"), AssemblyBuilderAccess.RunAndSave);

        private static ModuleBuilder moduleBuilder = null;
        private static ModuleBuilder ModuleBuilder => moduleBuilder ??= Assembly.DefineDynamicModule("DubsDynamicTypes", "DubsDynamicTypes.dll");

        public static Dictionary<string, HashSet<MethodInfo>> methods = new Dictionary<string, HashSet<MethodInfo>>();

        public static Type CreateType(string name, HashSet<MethodInfo> methods)
        {
            TypeBuilder tb = ModuleBuilder.DefineType(name, staticAtt, typeof(Entry));

            FieldBuilder active = tb.DefineField("Active", typeof(bool), FieldAttributes.Public | FieldAttributes.Static);

            if(methods != null) DynamicTypeBuilder.methods.Add(name, methods);
            else DynamicTypeBuilder.methods.Add(name, new HashSet<MethodInfo>());
            ConstructorBuilder ivCtor = tb.DefineConstructor(MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, new Type[0]);

            // default initialise active to false.
            ILGenerator ctorIL = ivCtor.GetILGenerator();
            ctorIL.Emit(OpCodes.Ldc_I4_0);
            ctorIL.Emit(OpCodes.Stsfld, active);
            ctorIL.Emit(OpCodes.Ret);

            CreateProfilePatch(name, tb);

            return tb.CreateType();
        }

        // We already have our methods defined inside a dictionary, all we need to do is index into it at runtime
        private static void CreateProfilePatch(string name, TypeBuilder tb)
        {
            MethodInfo func = AccessTools.Method(typeof(DynamicTypeBuilder), "PatchAll");

            MethodBuilder ProfilePatch = tb.DefineMethod("GetPatchMethods", MethodAttributes.Public | MethodAttributes.Static, typeof(IEnumerable<MethodInfo>), null);

            ILGenerator generator = ProfilePatch.GetILGenerator();

            generator.Emit(OpCodes.Ldstr, name);
            generator.Emit(OpCodes.Call, func);
            generator.Emit(OpCodes.Ret);
        }

        public static IEnumerable<MethodInfo> PatchAll(string name)
        {
            if (methods.TryGetValue(name, out var meths))
            {
                foreach (MethodInfo meth in meths)
                    yield return meth;
            }
        }

    }

}
