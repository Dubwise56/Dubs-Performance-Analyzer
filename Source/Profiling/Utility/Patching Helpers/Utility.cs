using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
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


        public static void ClearPatchedCaches()
        {
            patchedAssemblies.Clear();
            patchedTypes.Clear();
            patchedMethods.Clear();

            H_HarmonyPatches.PatchedPres.Clear();
            H_HarmonyPatches.PatchedPosts.Clear();

            InternalMethodUtility.ClearCaches();
            MethodTransplanting.ClearCaches();
            TranspilerMethodUtility.ClearCaches();
        }

        /*
         * Utility
         */
        public static IEnumerable<string> GetSplitString(string name)
        {
            List<string> listStrLineElements = new List<string>();


            if (name.Contains(','))
            {
                string[] range = name.Split(',');
                range.Do(str => str.Trim());
                foreach (string str in range)
                {
                    foreach (string ret in GetSplitString(str))
                        yield return ret;
                }
                yield break;
            }
            if (name.Contains(';'))
            {
                string[] range = name.Split(';');
                range.Do(str => str.Trim());
                foreach (string str in range)
                {
                    foreach (string ret in GetSplitString(str))
                        yield return ret;
                }
                yield break;
            }

            // check if our name has a ':', indicating a method
            if (name.Contains(':'))
            {
                yield return name.Trim();
                yield break;
            }

            if (name.Contains('.'))
            {
                if (name.CharacterCount('.') == 1)
                {
                    if (AccessTools.TypeByName(name) != null) // namespace.type
                    {
                        yield return name.Trim();
                        yield break;
                    }
                    else // type.method -> type:method
                    {
                        yield return name.Replace(".", ":").Trim();
                        yield break;
                    }
                }
                else
                {
                    if (AccessTools.TypeByName(name) != null) // namespace.type.type2 or namespace.namespace2.type etc
                    {
                        yield return name.Trim();
                        yield break;
                    }
                    else
                    {
                        // namespace.type.method
                        int ind = name.LastIndexOf('.');
                        yield return name.Remove(ind, 1).Insert(ind, ":").Trim();
                        yield break;
                    }
                }
            }
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
            ThreadSafeLogger.Error($"[Analyzer] Patching warning occured: {message}");
#endif
#if NDEBUG
            if (!displayMessages) return;
            ThreadSafeLogger.Warning($"[Analyzer] Patching notification: {message}");
#endif
        }
        private static void Error(string message)
        {
#if DEBUG
            ThreadSafeLogger.Error($"[Analyzer] Patching error occured: {message}");
#endif
#if NDEBUG
            if (!displayMessages) return;
            ThreadSafeLogger.Error($"[Analyzer] Patching error occured: {message}");
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

            if (!method.HasMethodBody())
            {
                Error("Does not have a methodbody");
                return false;
            }

            if (method.IsGenericMethod || method.ContainsGenericParameters)
            {
                Error("Can not currently patch generic methods");
                return false;
            }

            return true;
        }

        public static bool IsNotAnalyzerPatch(string patchId)
        {
            return patchId != Modbase.Harmony.Id && patchId != Modbase.StaticHarmony.Id;
        }


        public static IEnumerable<MethodInfo> GetTypeMethods(Type type)
        {
            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type))
                if (ValidMethod(method)) yield return method;
        }

        public static IEnumerable<MethodInfo> SubClassImplementationsOf(Type baseType, Func<MethodInfo, bool> predicate)
        {
            return baseType.AllSubclasses().SelectMany(t => AccessTools.GetDeclaredMethods(t).Where(m => predicate(m)));
        }

        public static IEnumerable<MethodInfo> SubClassNonAbstractImplementationsOf(Type baseType, Func<MethodInfo, bool> predicate)
        {
            return baseType.AllSubclassesNonAbstract().SelectMany(t => AccessTools.GetDeclaredMethods(t).Where(m => predicate(m)));
        }

        public static IEnumerable<MethodInfo> GetAssemblyMethods(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
                if (type.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
                    foreach (var m in GetTypeMethods(type)) yield return m;
        }




        // Unpatching

        public static void UnpatchMethod(string name) => UnpatchMethod(AccessTools.Method(name));
        public static void UnpatchMethod(MethodInfo method)
        {
            foreach (MethodBase methodBase in Harmony.GetAllPatchedMethods())
            {
                Patches infos = Harmony.GetPatchInfo(methodBase);

                var allPatches = infos.Prefixes.Concat(infos.Postfixes, infos.Transpilers, infos.Finalizers);

                foreach (var patch in allPatches)
                {
                    if (patch.PatchMethod == method)
                    {
                        Modbase.Harmony.Unpatch(methodBase, method);
                        return;
                    }
                }
            }
            Warn("Failed to locate method to unpatch");
        }

        public static void UnpatchMethodsOnMethod(string name) => UnpatchMethodsOnMethod(AccessTools.Method(name));
        public static void UnpatchMethodsOnMethod(MethodInfo method) => Modbase.Harmony.Unpatch(method, HarmonyPatchType.All);

        public static void UnPatchTypePatches(string name) => UnPatchTypePatches    (AccessTools.TypeByName(name));
        public static void UnPatchTypePatches(Type type)
        {
            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type))
                UnpatchMethodsOnMethod(method);
        }


        /*
         * Internal Method Patching
         */

        public static void PatchInternalMethod(string name)
        {
            MethodInfo method = null;
            try
            {
                method = AccessTools.Method(name);
            }
            catch (Exception e)
            {
                Error($"Failed to locate method {name}, errored with the message {e.Message}");
                return;
            }
            PatchInternalMethod(method);
        }
        public static void PatchInternalMethod(MethodInfo method)
        {
            if (InternalMethodUtility.PatchedInternals.Contains(method))
            {
                Warn("Trying to re-transpile an already profiled internal method");
                return;
            }
            PatchInternalMethodFull(method);
        }
        private static void PatchInternalMethodFull(MethodInfo method)
        {
            try
            {
                GUIController.AddEntry(method.Name + "-int", Category.Update);
                GUIController.SwapToEntry(method.Name + "-int");

                InternalMethodUtility.PatchedInternals.Add(method);

                Task.Factory.StartNew(() => Modbase.Harmony.Patch(method, transpiler: InternalMethodUtility.InternalProfiler));
            }
            catch (Exception e)
            {
                Error("Failed to patch internal methods, failed with the error " + e.Message);
                InternalMethodUtility.PatchedInternals.Remove(method);
            }
        }

        public static void PatchAssembly(string name)
        {
            ModContentPack mod = LoadedModManager.RunningMods.FirstOrDefault(m => m.Name == name || m.PackageId == name.ToLower());

            if (mod != null)
            {
                Notify($"Assembly count: { mod.assemblies?.loadedAssemblies?.Count ?? 0}");
                foreach (Assembly ass in mod.assemblies?.loadedAssemblies)
                    Notify($"Assembly named: {ass.FullName}, located at {ass.Location}");
            }


            IEnumerable<Assembly> assembly = mod?.assemblies?.loadedAssemblies?.Where(w => !w.FullName.Contains("Harmony") && !w.FullName.Contains("0MultiplayerAPI"));

            if (assembly != null && assembly.Count() != 0)
            {
                GUIController.AddEntry(mod.Name + "-prof", Category.Update);
                GUIController.SwapToEntry(mod.Name + "-prof");

                Task.Factory.StartNew( () => PatchAssemblyFull(mod.Name + "-prof", assembly.ToList()));
            }
            else
            {
                Error($"Failed to patch {name}");
            }
        }
        private static void PatchAssemblyFull(string key, List<Assembly> assemblies)
        {
            var meths = new HashSet<MethodInfo>();

            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    if (patchedAssemblies.Contains(assembly.FullName))
                    {
                        Warn($"patching {assembly.FullName} failed, already patched");
                        return;
                    }
                    patchedAssemblies.Add(assembly.FullName);

                    foreach (Type type in assembly.GetTypes())
                    {
                        if (type.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() == null)
                        {
                            if (patchedTypes.Contains(type.FullName))
                            {
                                Warn($"patching {type.FullName} failed, already patched");
                                return;
                            }
                            patchedTypes.Add(type.FullName);

                            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type))
                            {

                                if (!patchedMethods.Contains(method.Name) && method.DeclaringType == type)
                                {
                                    try
                                    {
                                        if (ValidMethod(method))
                                            meths.Add(method);
                                    }
                                    catch (Exception e)
                                    {
                                        Warn($"Failed to log method {method.Name} errored with the message {e.Message}");
                                    }
                                    patchedMethods.Add(method.Name);
                                }
                            }
                        }
                    }

                    Notify($"Patched {assembly.FullName}");
                }
                catch (Exception e)
                {
                    Error($"catch. patching {assembly.FullName} failed, {e.Message}");
                }
            }

            MethodTransplanting.UpdateMethods(AccessTools.TypeByName(key), meths);
        }

    }
}
