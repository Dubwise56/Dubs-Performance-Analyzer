using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace DubsAnalyzer
{
 /*
  * Todo
  */
    public static class DynamicTypeBuilder
    {
        static AssemblyBuilder assembly = null;
        static TypeAttributes staticAtt = TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;
        // this is what a static class looks like in memory
        static AssemblyBuilder Assembly
        { 
            get
            {
                if(assembly == null)
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
                if(moduleBuilder == null)
                {
                    moduleBuilder = Assembly.GetDynamicModule("Main Module");
                }
                return moduleBuilder;
            }
        }

        public static Dictionary<string, List<MethodInfo>> methods = new Dictionary<string, List<MethodInfo>>();

        // MethodBase : __originalMethod
        // object : __instance
        // return : whether we cancel this object or not
        public static void CreateType(string name, Func<MethodBase, object, bool> prefixAction, List<MethodInfo> methods = null)
        {
            TypeBuilder tb = ModuleBuilder.DefineType(name, staticAtt, null);

        }

        public static void CreateType(string name, Func<MethodBase, object, bool> prefixAction, Func<IEnumerable<MethodInfo>> getMethods = null)
        {
            TypeBuilder tb = ModuleBuilder.DefineType(name, staticAtt, null);

            FieldBuilder activeField = tb.DefineField("Active", typeof(bool), FieldAttributes.Static | FieldAttributes.Public);

            CreatePrefix(tb);
        }

        // We already have our methods defined inside a dictionary, all we need to do is index into it at runtime
        private static void CreateProfileTargetter(string name)
        {

        }
        // We have an action which can scoop our methods
        private static void CreateProfileTargetter(Func<IEnumerable<MethodInfo>> getMethods)
        {

        }

        private static void CreatePrefix(TypeBuilder tb)
        {
            MethodBuilder prefix = tb.DefineMethod("Prefix", MethodAttributes.Public | MethodAttributes.Static, typeof(bool), new Type[] { typeof(MethodBase), typeof(object), typeof(string).MakeByRefType() });

            // name our params as such as per Harmony's constants: https://github.com/pardeike/Harmony/blob/master/Harmony/Internal/MethodPatcher.cs#L13-L21
            prefix.DefineParameter(1, ParameterAttributes.In, "__originalMethod");
            prefix.DefineParameter(2, ParameterAttributes.In, "__instance");
            prefix.DefineParameter(3, ParameterAttributes.In, "__state");

            var generator = prefix.GetILGenerator();
            var skipLabel = generator.DefineLabel();

            // if(Active && AnalyzerState.CurrentlyRunning)
            generator.Emit(OpCodes.Ldsfld, AccessTools.Field(prefix.DeclaringType, "Active"));
            generator.Emit(OpCodes.Brfalse_S, skipLabel);

            generator.Emit(OpCodes.Ldsfld, AccessTools.Field(typeof(AnalyzerState), "CurrentlyRunning"));
            generator.Emit(OpCodes.Brfalse_S, skipLabel);

            // if we have a Func<> we can call it here, and return the value it returns
            // if not, we look for the  dictionary 'Methods' and access it with our Types name
            // update the __state parameter to the __originalMethod.Name
            // call Analyzer.Start(__state, null, null, null, __originalMethod as MethodInfo);

            // ...
            // }
            // return false;
            generator.Emit(OpCodes.Nop);
            generator.MarkLabel(skipLabel);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Ret);
        }

        private static void CreatePostfix()
        {

        }

    }

}
