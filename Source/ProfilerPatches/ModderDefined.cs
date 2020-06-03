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
using Verse;

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

        public delegate bool PrefixMethod(MethodBase method, object instance, params object[] args);

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
                    moduleBuilder = Assembly.GetDynamicModule("Main Module");
                }
                return moduleBuilder;
            }
        }

        public static Dictionary<string, List<MethodInfo>> methods = new Dictionary<string, List<MethodInfo>>();

        public static void CreateType(string name, List<MethodInfo> methods, PrefixMethod prefixAction, params string[] additionalParamNames)
        {
            TypeBuilder tb = ModuleBuilder.DefineType(name, staticAtt, null);

        }
        public static void CreateType(string name, Func<IEnumerable<MethodInfo>> getMethods, PrefixMethod prefixAction)
        {
            TypeBuilder tb = ModuleBuilder.DefineType(name, staticAtt, null);

            FieldBuilder activeField = tb.DefineField("Active", typeof(bool), FieldAttributes.Static | FieldAttributes.Public);

            CreatePrefix(tb, prefixAction);
        }

        // We already have our methods defined inside a dictionary, all we need to do is index into it at runtime
        private static void CreateProfileTargetter(string name)
        {

        }
        // We have an action which can scoop our methods
        private static void CreateProfileTargetter(Func<IEnumerable<MethodInfo>> getMethods)
        {

        }

        private static void CreatePrefix(TypeBuilder tb, PrefixMethod method)
        {
            MethodInfo dynMethod = method.Method;

            List<Type> parameters = new List<Type>();
            List<string> paramNames = new List<string>();
            foreach (var param in dynMethod.GetParameters())
            {
                parameters.Add(param.ParameterType);
                paramNames.Add(param.Name);
            }

            MethodBuilder prefix = tb.DefineMethod("Prefix", MethodAttributes.Public | MethodAttributes.Static, typeof(bool), parameters.ToArray());

            MethodInfo MethodInfoEquality = AccessTools.Method(typeof(MethodInfo), "op_Equality");
            // name our params as such as per Harmony's constants: https://github.com/pardeike/Harmony/blob/master/Harmony/Internal/MethodPatcher.cs#L13-L21

            for (int i = 0; i < dynMethod.GetParameters().Length; i++)
                prefix.DefineParameter(i, ParameterAttributes.In, paramNames[i]);

            var generator = prefix.GetILGenerator();
            var skipLabel = generator.DefineLabel();
            var regularLabel = generator.DefineLabel();
            var skipRegularLabel = generator.DefineLabel();

            // if(Active && AnalyzerState.CurrentlyRunning)
            generator.Emit(OpCodes.Ldsfld, AccessTools.Field(prefix.DeclaringType, "Active"));
            generator.Emit(OpCodes.Brfalse_S, skipLabel);

            generator.Emit(OpCodes.Ldsfld, AccessTools.Field(typeof(AnalyzerState), "CurrentlyRunning"));
            generator.Emit(OpCodes.Brfalse_S, skipLabel);

            // we now need to check if we are the the 'current' method, if so we
            // will get additional information, including bytes, stacktrace

            // if(__originalMethod as MethodInfo == CurrentMethod) { } else { } 
            // 
            generator.Emit(OpCodes.Ldarg_1); // __originalMethod
            generator.Emit(OpCodes.Isinst, typeof(MethodInfo)); // __originalMethod as MethodInfo

            //generator.Emit(OpCodes.ldsfld, ...) // get our 'current' method
            generator.Emit(OpCodes.Call, MethodInfoEquality); // equality
            generator.Emit(OpCodes.Brfalse_S, regularLabel);

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
