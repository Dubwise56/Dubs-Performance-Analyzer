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

namespace Analyzer
{
    public static class Utility
    {
        public static List<string> patchedAssemblies = new List<string>();
        public static List<string> patchedTypes = new List<string>();
        public static List<string> patchedMethods = new List<string>();

        private static Thread patchAssemblyThread = null;
        private static Thread patchTypeThread = null;

        public static bool displayMessages => Settings.verboseLogging;


        public static void ClearPatchedCaches()
        {
            patchedAssemblies.Clear();
            patchedTypes.Clear();
            patchedMethods.Clear();

            H_HarmonyPatches.PatchedPres.Clear();
            H_HarmonyPatches.PatchedPosts.Clear();
            H_HarmonyTranspilers.PatchedMeths.Clear();

            UnpatchAllInternalMethods();
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
        private static bool ValidMethod(MethodInfo method)
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

        // returns true if the method is null, if(GetMethod(.., .., ..)) return;
        private static bool GetMethod(string name, out MethodInfo methodInfo)
        {
            methodInfo = null;
            try
            {
                methodInfo = AccessTools.Method(name);
                return !ValidMethod(methodInfo);
            }
            catch (Exception e)
            {
                Error($"failed to locate method {name}, errored with the message {e.Message}");
                return true;
            }
        }

        /*
         * Method Patching
         */
        public static void PatchMethod(string name, HarmonyMethod pre, HarmonyMethod post)
        {
            if (GetMethod(name, out MethodInfo method))
                return;

            PatchMethod(method, pre, post);
        }
        public static void PatchMethod(MethodInfo method, HarmonyMethod pre, HarmonyMethod post)
        {
            PatchMethodFull(method, pre, post);
        }
        private static void PatchMethodFull(MethodInfo method, HarmonyMethod pre, HarmonyMethod post)
        {
            if (patchedMethods.Contains(method.Name))
            {
                Warn($"patching {method.Name} failed, already patched");
                return;
            }

            patchedMethods.Add(method.Name);
            try
            {
                Modbase.Harmony.Patch(method, pre, post);
            }
            catch (Exception e)
            {
                Error($"Failed to log method {method.Name} errored with the message {e.Message}");
            }

            Notify($"patching {method.Name} succeeded");
        }

        public static IEnumerable<MethodInfo> GetMethodsPatchingMethod(string name)
        {
            if (GetMethod(name, out MethodInfo method))
                return null;

            return GetMethodsPatchingMethod(method);
        }
        public static IEnumerable<MethodInfo> GetMethodsPatchingMethod(MethodInfo method)
        {
            Patches patches = Harmony.GetPatchInfo(method);

            foreach (var m in patches.Prefixes.Select(p => p.PatchMethod))
                if (ValidMethod(m)) yield return m;
            foreach (var m in patches.Postfixes.Select(p => p.PatchMethod))
                if (ValidMethod(m)) yield return m;
            foreach (var m in patches.Transpilers.Select(p => p.PatchMethod))
                if (ValidMethod(m)) yield return m;
            foreach (var m in patches.Finalizers.Select(p => p.PatchMethod))
                if (ValidMethod(m)) yield return m;
        }
        public static void PatchMethodPatches(string name, HarmonyMethod pre, HarmonyMethod post)
        {
            if (GetMethod(name, out MethodInfo method))
                return;

            PatchMethodPatches(method, pre, post);
        }
        public static void PatchMethodPatches(MethodInfo method, HarmonyMethod pre, HarmonyMethod post)
        {
            foreach (var meth in GetMethodsPatchingMethod(method))
                PatchMethodFull(meth, pre, post);
        }

        /*
         * Method Unpatching
         */
        public static void UnpatchMethod(string name)
        {
            if (GetMethod(name, out MethodInfo method))
                return;

            UnpatchMethod(method);
        }
        public static void UnpatchMethod(MethodInfo method)
        {
            UnpatchMethodFull(method);
        }
        private static void UnpatchMethodFull(MethodInfo method)
        {
            foreach (MethodBase methodBase in Harmony.GetAllPatchedMethods())
            {
                Patches infos = Harmony.GetPatchInfo(methodBase);

                var allPatches = infos.Prefixes.Concat(infos.Postfixes, infos.Transpilers, infos.Finalizers);

                foreach(var patch in allPatches)
                {
                    if(patch.PatchMethod == method)
                    { 
                        Modbase.Harmony.Unpatch(methodBase, method);
                        return;
                    }
                }
            }
            Warn("Failed to locate method to unpatch");
        }
        public static void UnpatchMethodsOnMethod(string name)
        {
            if (GetMethod(name, out MethodInfo method))
                return;

            UnpatchMethodsOnMethod(method);
        }
        public static void UnpatchMethodsOnMethod(MethodInfo method)
        {
            UnpatchMethodsOnMethodFull(method);
        }
        private static void UnpatchMethodsOnMethodFull(MethodInfo method)
        {
            Modbase.Harmony.Unpatch(method, HarmonyPatchType.All);
        }

        /*
         * Type Patching
         */

        public static IEnumerable<MethodInfo> GetTypeMethods(string name)
        {
            Type type = null;
            try
            {
                type = AccessTools.TypeByName(name);
            }
            catch (Exception e)
            {
                Error($"Failed to locate type {name}, errored with the message {e.Message}");
                return null;
            }

            return GetTypeMethods(type);
        }
        public static IEnumerable<MethodInfo> GetTypeMethods(Type type)
        {
            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type))
            {
                if (method.DeclaringType == type && !method.IsSpecialName && !method.IsAssembly && method.HasMethodBody())
                {
                    yield return method;
                }
            }
        }
        public static void PatchType(string name, HarmonyMethod pre, HarmonyMethod post)
        {
            Type type = null;
            try
            {
                type = AccessTools.TypeByName(name);
            }
            catch (Exception e)
            {
                Error($"Failed to locate type {name}, errored with the message {e.Message}");
                return;
            }

            PatchType(type, pre, post);
        }
        public static void PatchType(Type type, HarmonyMethod pre, HarmonyMethod post)
        {
            patchTypeThread = new Thread(() => PatchTypeFull(type, pre, post));
            patchTypeThread.Start();
        }
        private static void PatchTypeFull(Type type, HarmonyMethod pre, HarmonyMethod post)
        {
            try
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
                                Modbase.Harmony.Patch(method, pre, post);
                        }
                        catch (Exception e)
                        {
                            Warn($"Failed to log method {method.Name} errored with the message {e.Message}");
                        }
                        patchedMethods.Add(method.Name);
                    }
                }
                Notify($"Patched {type.FullName}");
            }
            catch (Exception e)
            {
                Error($"catch. patching {type.FullName} failed, {e.Message}");
            }
        }
        public static void PatchTypePatches(string name, HarmonyMethod pre, HarmonyMethod post)
        {
            Type type = null;
            try
            {
                type = AccessTools.TypeByName(name);
            }
            catch (Exception e)
            {
                Error($"Failed to locate type {name}, errored with the message {e.Message}");
                return;
            }

            PatchTypePatchesFull(type, pre, post);
        }
        private static void PatchTypePatchesFull(Type type, HarmonyMethod pre, HarmonyMethod post)
        {
            try
            {
                foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type))
                {
                    foreach (var meth in GetMethodsPatchingMethod(method))
                        PatchMethodFull(meth, pre, post);
                }

                Notify($"Sucessfully Patched the methods patching {type.FullName}");
            }
            catch (Exception e)
            {
                Error($"patching {type.FullName} failed, {e.Message}");
            }
        }

        /*
         * Type Unpatching
         */

        public static void UnPatchTypePatches(string name)
        {
            Type type = null;
            try
            {
                type = AccessTools.TypeByName(name);
            }
            catch (Exception e)
            {
                Error($"Failed to locate type {name}, errored with the message {e.Message}");
                return;
            }

            UnPatchTypePatches(type);
        }
        public static void UnPatchTypePatches(Type type)
        {
            if (type == null)
            {
                Error("Cannnot unpatch null");
                return;
            }

            UnPatchTypePatchesFull(type);
        }
        private static void UnPatchTypePatchesFull(Type type)
        {
            try
            {
                foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type))
                {
                    UnpatchMethodsOnMethod(method);
                }

                Notify($"Sucessfully unpatched the methods patching {type.FullName}");
            }
            catch (Exception e)
            {
                Error($"unpatching {type.FullName} failed, {e.Message}");
            }
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

                InternalMethodUtility.curMeth = method;
                InternalMethodUtility.PatchedInternals.Add(method);

                Task.Factory.StartNew( () => InternalMethodUtility.Harmony.Patch(method, null, null, InternalMethodUtility.InternalProfiler));
            }
            catch (Exception e)
            {
                Error("Failed to patch internal methods, failed with the error " + e.Message);
                InternalMethodUtility.PatchedInternals.Remove(method);
            }
        }

        public static void UnpatchInternalMethod(string name)
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
            UnpatchInternalMethod(method);
        }
        public static void UnpatchInternalMethod(MethodInfo method)
        {
            if (!InternalMethodUtility.PatchedInternals.Contains(method))
            {
                Warn($"There is no method with the name {method.Name} that has been noted as profiled");
                return;
            }
            UnpatchInternalMethodFull(method);
        }
        private static void UnpatchInternalMethodFull(MethodInfo method)
        {
            InternalMethodUtility.curMeth = method;
            InternalMethodUtility.Harmony.Unpatch(method, HarmonyPatchType.Transpiler, InternalMethodUtility.Harmony.Id);
            InternalMethodUtility.PatchedInternals.Remove(method);
        }

        public static void UnpatchAllInternalMethods()
        {
            UnpatchAllInternalMethodsFull();
        }
        private static void UnpatchAllInternalMethodsFull()
        {
            InternalMethodUtility.Harmony.UnpatchAll(InternalMethodUtility.Harmony.Id);
            InternalMethodUtility.PatchedInternals.Clear();
            InternalMethodUtility.KeyMethods.Clear();
            InternalMethodUtility.curMeth = null;
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

                HarmonyMethod pre = new HarmonyMethod(AccessTools.TypeByName(mod.Name + "-prof").GetMethod("Prefix", BindingFlags.Public | BindingFlags.Static));
                HarmonyMethod post = new HarmonyMethod(AccessTools.TypeByName(mod.Name + "-prof").GetMethod("Postfix", BindingFlags.Public | BindingFlags.Static));

                patchAssemblyThread = new Thread(() => PatchAssemblyFull(assembly.ToList(), pre, post));
                patchAssemblyThread.Start();
            }
            else
            {
                Error($"Failed to patch {name}");
            }
        }
        private static void PatchAssemblyFull(List<Assembly> assemblies, HarmonyMethod pre, HarmonyMethod post)
        {
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
                            PatchTypeFull(type, pre, post);
                    }


                    Notify($"Patched {assembly.FullName}");
                }
                catch (Exception e)
                {
                    Error($"catch. patching {assembly.FullName} failed, {e.Message}");
                }
            }
        }

    }
}
