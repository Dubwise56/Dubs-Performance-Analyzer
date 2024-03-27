using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Verse;

namespace Analyzer.Profiling
{
    public static class Utility
    {
        public static List<string> patchedAssemblies = new List<string>();
        public static List<string> patchedTypes = new List<string>();
        public static List<string> patchedMethods = new List<string>();

        public static bool displayMessages => Settings.verboseLogging;


        public static void ClearPatchCaches()
        {
            patchedAssemblies.Clear();
            patchedTypes.Clear();
            patchedMethods.Clear();

            InternalMethodUtility.ClearCaches();
            MethodTransplanting.ClearCaches();
            TranspilerMethodUtility.ClearCaches();

            MethodInfoCache.ClearCache();

#if DEBUG
            ThreadSafeLogger.Message("[Analyzer] Cleared all caches");
#endif
        }

        /*
         * Utility
         */
        public static IEnumerable<string> GetSplitString(string name)
        {

            IEnumerable<string> HandleMultipleMethods(char seperator)
            {
                if (name.Contains(seperator) == false) yield break;
                
                var range = name.Split(seperator);
                foreach (var str in range)
                {
                    yield return str.Trim();
                }
            }

            foreach (var str in HandleMultipleMethods(','))
                yield return str;
            
            foreach (var str in HandleMultipleMethods(';'))
                yield return str;


            yield return name;
        }
        
        internal static string GetSignature(MethodBase method, bool showParameters = true)
        {
            var firstParam = true;
            var sigBuilder = new StringBuilder(40);

            string mKey;
            if (method.ReflectedType != null) mKey = TypeName(method.ReflectedType, true) + ":" + method.Name;
            else mKey = TypeName(method.DeclaringType, true) + ":" + method.Name;
            sigBuilder.Append(mKey);

            // Add method generics
            if(method.IsGenericMethod)
            {
                sigBuilder.Append("<");
                foreach(var g in method.GetGenericArguments())
                {
                    if (firstParam) firstParam = false;
                    else sigBuilder.Append(", ");

                    sigBuilder.Append(TypeName(g));
                }
                sigBuilder.Append(">");
            }


            if (showParameters)
            {
                sigBuilder.Append("(");

                firstParam = true;
                foreach (var param in method.GetParameters())
                {
                    if (firstParam)
                    {
                        firstParam = false;
                        if (method.IsDefined(typeof(ExtensionAttribute), false))
                        {
                            sigBuilder.Append("this ");
                        }
                    }
                    else
                        sigBuilder.Append(", ");

                    if (param.ParameterType.IsByRef)
                        sigBuilder.Append("ref ");
                    else if (param.IsOut)
                        sigBuilder.Append("out ");

                    sigBuilder.Append(TypeName(param.ParameterType));
                }
                sigBuilder.Append(")");
            }


            return sigBuilder.ToString();
        }

        internal static bool IsGenericType(Type type)
        {
            return (type.GetGenericArguments()?.Any() ?? false) && type.FullName.Contains('`');
        }


        internal static string TypeName(Type type, bool fullName = false)
        {
            var nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null) return nullableType.Name + "?";

            var tName = fullName ? type.FullName : type.Name;

            if (IsGenericType(type))
            {
                var sb = new StringBuilder(tName.Substring(0, tName.IndexOf('`')));
                sb.Append('<');
                var first = true;
                foreach (var t in type.GenericTypeArguments)
                {
                    if (!first)
                        sb.Append(", ");

                    sb.Append(TypeName(t));
                    first = false;
                }
                sb.Append('>');
                return sb.ToString();
            }
            else
            {
                string ReplaceOccurence(string typeName, string to)
                {
                    return type.Name.Replace(typeName, to);
                }

                // This finds things like "String[]" as well as just String, which is why its not in the switch
                if (type.Name.Contains("String")) return ReplaceOccurence("String", "string");
                if (type.Name.Contains("Int32")) return ReplaceOccurence("Int32", "int");
                if (type.Name.Contains("Object")) return ReplaceOccurence("Object", "object");
                if (type.Name.Contains("Boolean"))  return ReplaceOccurence("Boolean", "bool");
                if (type.Name.Contains("Decimal")) return ReplaceOccurence("Decimal", "decimal");

                if (type.Name == "Void") return "void"; 

                return fullName ? type.FullName : type.Name;
            }
        }
    

        public static string GetMethodKey(MethodBase meth)
        {
            return GetSignature(meth, Settings.longFormNames);
        }

        private static void Notify(string message)
        {
#if DEBUG
            ThreadSafeLogger.Error($"[Analyzer] Patching notification: {message}");
#endif
#if NDEBUG
            if (!displayMessages) return;
            ThreadSafeLogger.Message($"[Analyzer] Patching notification: {message}");
#endif
        }

        private static void Warn(string message)
        {
#if DEBUG
            ThreadSafeLogger.Error($"[Analyzer] Patching warning: {message}");
#endif
#if NDEBUG
            if (!displayMessages) return;
            ThreadSafeLogger.Warning($"[Analyzer] Patching warning: {message}");
#endif
        }

        private static void Error(string message)
        {
#if DEBUG
            ThreadSafeLogger.Error($"[Analyzer] Patching error: {message}");
#endif
#if NDEBUG
            if (!displayMessages) return;
            ThreadSafeLogger.Error($"[Analyzer] Patching error: {message}");
#endif
        }

        private static void ReportException(Exception e, string message)
        {
#if DEBUG
            ThreadSafeLogger.ReportException(e, $"[Analyzer] Patching error: {message}");
#endif
#if NDEBUG
            if (!displayMessages) return;
            ThreadSafeLogger.ReportException(e, message);
#endif
        }

        // returns false is the method is invalid
        public static bool ValidMethod(MethodInfo method)
        {
            if (method == null)
            {
                Error("Null MethodInfo");
                return false;
            }

            var mKey = method?.DeclaringType?.FullName + ":" + method.Name;

            if (!method.HasMethodBody())
            {
                return false;
            }

            if (method.IsGenericMethod || method.ContainsGenericParameters)
            {
                return false;
            }

            if (patchedMethods.Contains(mKey))
            {
                return false;
            }

            patchedMethods.Add(mKey);

            return true;
        }

        public static bool IsNotAnalyzerPatch(string patchId)
        {
            return patchId != Modbase.Harmony.Id && patchId != Modbase.StaticHarmony.Id;
        }

        public static IEnumerable<MethodInfo> GetMethods(string str)
        {
            foreach (var s in GetSplitString(str))
                yield return AccessTools.Method(s);
        }

        public static IEnumerable<MethodInfo> GetMethodsPatching(string str)
        {
            foreach (var meth in GetMethods(str))
            {
                var p = Harmony.GetPatchInfo(meth);

                foreach (var patch in p.Prefixes.Concat(p.Postfixes, p.Transpilers, p.Finalizers))
                    yield return patch.PatchMethod;
            }
        }

        public static IEnumerable<MethodInfo> GetMethodsPatchingType(Type type)
        {
            foreach (var meth in GetTypeMethods(type))
            {
                var p = Harmony.GetPatchInfo(meth);

                foreach (var patch in p.Prefixes.Concat(p.Postfixes, p.Transpilers, p.Finalizers))
                    yield return patch.PatchMethod;
            }
        }


        public static IEnumerable<MethodInfo> GetTypeMethods(Type type, bool nestedClasses = false)
        {
            foreach (var method in AccessTools.GetDeclaredMethods(type).Where(ValidMethod))
                yield return method;

            if (!nestedClasses) yield break;
            
            foreach(var t in type.GetNestedTypes(AccessTools.all))
                foreach (var m in GetTypeMethods(t, true))
                    yield return m;
        }

        public static IEnumerable<MethodInfo> SubClassImplementationsOf(Type baseType, Func<MethodInfo, bool> predicate)
        {
            var meths = new List<MethodInfo>();
            foreach (var t in baseType.AllSubclasses())
            {
                meths.AddRange(GetTypeMethods(t).Where(predicate));
            }

            return meths;
        }

        public static IEnumerable<MethodInfo> SubClassNonAbstractImplementationsOf(Type baseType, Func<MethodInfo, bool> predicate)
        {
            var meths = new List<MethodInfo>();
            foreach (var t in baseType.AllSubclassesNonAbstract())
            {
                meths.AddRange(GetTypeMethods(t).Where(predicate));
            }

            return meths;
        }

        public static IEnumerable<MethodInfo> GetAssemblyMethods(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes().Where(t => t.GetCustomAttribute<CompilerGeneratedAttribute>() == null))
                    foreach (var m in GetTypeMethods(type))
                        yield return m;
        }


        // Unpatching

        public static void UnpatchMethod(string name) => UnpatchMethod(AccessTools.Method(name));

        public static void UnpatchMethod(MethodInfo method)
        {
            foreach (MethodBase methodBase in Harmony.GetAllPatchedMethods())
            {
                HarmonyLib.Patches infos = Harmony.GetPatchInfo(methodBase);

                var allPatches = infos.Prefixes.Concat(infos.Postfixes, infos.Transpilers, infos.Finalizers);

                if (!allPatches.Any(patch => patch.PatchMethod == method)) continue;

                Modbase.Harmony.Unpatch(methodBase, method);
                return;
            }

            Warn("Failed to locate method to unpatch");
        }

        public static void UnpatchMethodsOnMethod(string name) => UnpatchMethodsOnMethod(AccessTools.Method(name));
        public static void UnpatchMethodsOnMethod(MethodInfo method) => Modbase.Harmony.Unpatch(method, HarmonyPatchType.All);

        public static void UnPatchTypePatches(string name) => UnPatchTypePatches(AccessTools.TypeByName(name));

        public static void UnPatchTypePatches(Type type)
        {
            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type))
                UnpatchMethodsOnMethod(method);
        }


        /*
         * Internal Method Patching
         */

        public static void PatchInternalMethod(string name, Category category)
        {
            MethodInfo method = null;
            try
            {
                method = AccessTools.Method(name);
            }
            catch (Exception e)
            {
                Messages.Message($"Failed to find the method(s) represented by {name}", MessageTypeDefOf.CautionInput, false);

                ReportException(e, $"Failed to locate the method {name}");
                return;
            }

            PatchInternalMethod(method, category);
        }

        public static void PatchInternalMethod(MethodInfo method, Category category)
        {
            if (InternalMethodUtility.PatchedInternals.Contains(method))
            {
                var ms = GetSignature(method, true);
                Messages.Message($"Have already patched the method {ms}", MessageTypeDefOf.CautionInput, false);
                Warn($"Trying to re-transpile an already profiled internal method - {ms}");
                return;
            }

            PatchInternalMethodFull(method, category);
        }

        private static void PatchInternalMethodFull(MethodInfo method, Category category)
        {
            try
            {
                bool Valid()
                {
                    var bytes = method.GetMethodBody()?.GetILAsByteArray();
                    if (bytes == null) return false;
                    if (bytes.Length == 0) return false;
                    if (bytes.Length == 1 && bytes.First() == 0x2A) return false;
                    return true;
                }

                if (Valid() == false)
                {
                    Error("Can not patch this method, this is likely a method which is virtually dispatched or marked as external, and thus can not be generically examined.");
                    return;
                }

                var guiEntry = method.DeclaringType + ":" + method.Name + "-int";
                GUIController.AddEntry(guiEntry, category);
                GUIController.SwapToEntry(guiEntry);

                InternalMethodUtility.PatchedInternals.Add(method);

                var work = () => {
                    try
                    {
                        Modbase.Harmony.Patch(method, transpiler: InternalMethodUtility.InternalProfiler);
                    }
                    catch (Exception e)
                    {
                        ReportException(e, $"Failed to patch the internal methods within {Utility.GetSignature(method, false)}");
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
            catch (Exception e)
            {
                ReportException(e, "Failed to set up state to patch internal methods");
                InternalMethodUtility.PatchedInternals.Remove(method);
            }
        }
        
        public static IEnumerable<Type> AllSubclassesAndBase(this Type t)
        {
            foreach (var type in t.AllSubclasses())
                yield return type;

            yield return t;
        }
        
        public static IEnumerable<MethodInfo> AllSubnBaseImplsOf(this Type t, Func<Type, MethodInfo> getMethod)
        {
            foreach (var type in t.AllSubclassesAndBase())
            {
                var method = getMethod(type);
                if (method.DeclaringType == type && method.HasMethodBody()) yield return method;
            }
        }
        
        private static bool ValidAssembly(Assembly assembly)
        {
            if (assembly.FullName.Contains("0Harmony")) return false;
            if (assembly.FullName.Contains("Cecil")) return false;
            if (assembly.FullName.Contains("Multiplayer")) return false;
            if (assembly.FullName.Contains("UnityEngine")) return false;

            return true;
        }

        public static void PatchAssembly(string name, Category type)
        {
            var mod = LoadedModManager.RunningMods.FirstOrDefault(m => m.Name == name || m.PackageId == name.ToLower());
            if (mod == null)
            {
                Error($"Failed to locate the mod {name}");
                return;
            }

            var assemblies = mod?.assemblies?.loadedAssemblies?.Where(ValidAssembly).ToList();

            if (assemblies != null && assemblies.Count() != 0)
            {
                GUIController.AddEntry(mod.Name + "-prof", type);
                GUIController.SwapToEntry(mod.Name + "-prof");

                if (Settings.disableThreadedPatching) 
                {
                    PatchAssemblyFull(mod.Name + "-prof", assemblies.ToList());
                } else
                {    
                    Task.Factory.StartNew(() => PatchAssemblyFull(mod.Name + "-prof", assemblies.ToList()));
                }
            }
            else
            {
                Error($"Failed to patch {name} - There are no assemblies");
            }
        }

        private static void PatchAssemblyFull(string key, List<Assembly> assemblies)
        {
            var meths = new HashSet<MethodInfo>();

            foreach (var asm in assemblies)
            {
                try
                {
                    var asmName = $"{asm.GetName().Name}.dll";
                    if (patchedAssemblies.Contains(asm.FullName))
                    {
                        Warn($"patching {asmName} failed, already patched");
                        continue;
                    }

                    patchedAssemblies.Add(asm.FullName);
                    
                    foreach (var type in AccessTools.GetTypesFromAssembly(asm))
                    {
                        var methods = AccessTools.GetDeclaredMethods(type).ToList();
                        foreach (var m in methods)
                        {
                            try
                            {
                                if (ValidMethod(m) && m.DeclaringType == type)
                                    meths.TryAdd(m);
                            }
                            catch (Exception e)
                            {
                                ReportException(e, $"Skipping method {GetSignature(m)}");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    ReportException(e, $"Failed to patch the assembly {asm.FullName}");
                }
            }

            var asms = assemblies.Select(a => a.GetName().Name + ".dll").Join();
            var asmString = assemblies.Count > 1 ? "assemblies" : "assembly";
            Messages.Message($"Attempting to patch {meths.Count} methods from the {asmString} [{asms}]", MessageTypeDefOf.CautionInput, false);

            MethodTransplanting.UpdateMethods(GUIController.types[key], meths);
        }
        
        // We do *NOT* check for methods that are empty, or pure virtual, as they can still be dispatched and timed
        // however we need do need to prevent them being internal patched.
        public static bool ValidCallInstruction(CodeInstruction cur, CodeInstruction prev, out MethodBase methodBase, out string key)
        {
            methodBase = null;
            key = null;

            // needs to be Call || CallVirt
            if ( !(cur.opcode == OpCodes.Call || cur.opcode == OpCodes.Callvirt)) return false;
            // can not be constrained
            if (prev != null && prev.opcode == OpCodes.Constrained) return false;

            methodBase = cur.operand as MethodBase;
            key = GetMethodKey(methodBase);

            // Make sure it is not an analyzer profiling method
            return !methodBase.DeclaringType.FullName.Contains("Analyzer.Profiling");
        }
    }
}