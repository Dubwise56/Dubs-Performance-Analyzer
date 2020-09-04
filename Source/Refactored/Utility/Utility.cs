using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Verse;

namespace Analyzer
{
    public static class Utility
    {
        public static List<string> PatchedAssemblies = new List<string>();
        public static List<string> PatchedTypes = new List<string>();
        public static List<string> PatchedMethods = new List<string>();

        private static Thread patchAssemblyThread = null;
        private static Thread patchTypeThread = null;

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
            lock (Dialog_Analyzer.messageSync)
            {
                Dialog_Analyzer.QueuedMessages.Add(delegate { Messages.Message(message, MessageTypeDefOf.PositiveEvent, false); });
            }
        }
        private static void Warn(string message)
        {
            lock (Dialog_Analyzer.messageSync)
            {
                Dialog_Analyzer.QueuedMessages.Add(delegate { Messages.Message(message, MessageTypeDefOf.CautionInput, false); });
            }
        }
        private static void Error(string message)
        {
            lock (Dialog_Analyzer.messageSync)
            {
                Dialog_Analyzer.QueuedMessages.Add(delegate { Messages.Message(message, MessageTypeDefOf.NegativeEvent, false); });
            }
        }


        private static bool ValidMethod(MethodInfo method, bool silence)
        {
            if (method == null)
            {
                if (!silence) Error("Null MethodInfo");
                return false;
            }

            if (!method.HasMethodBody())
            {
                if (!silence) Error("Does not have a methodbody");
                return false;
            }

            return true;
        }

        private static bool GetMethod(string name, bool silence, out MethodInfo methodInfo)
        {
            methodInfo = null;
            try
            {
                methodInfo = AccessTools.Method(name);
            }
            catch (Exception e)
            {
                if (!silence)
                    Error($"failed to locate method {name}, errored with the message {e.Message}");
                return true;
            }
            return false;
        }

        /*
         * Method Patching
         */
        public static void PatchMethod(string name, HarmonyMethod pre, HarmonyMethod post, bool display = true)
        {
            if (GetMethod(name, false, out MethodInfo method))
                return;

            PatchMethod(method, pre, post, display);
        }
        public static void PatchMethod(MethodInfo method, HarmonyMethod pre, HarmonyMethod post, bool display = true)
        {
            PatchMethodFull(method, pre, post, display);
        }
        private static void PatchMethodFull(MethodInfo method, HarmonyMethod pre, HarmonyMethod post, bool display = true)
        {
            if (PatchedMethods.Contains(method.Name))
            {
                if (display)
                    Warn($"patching {method.Name} failed, already patched");
                return;
            }

            PatchedMethods.Add(method.Name);
            try
            {
                Modbase.harmony.Patch(method, pre, post);
            }
            catch (Exception e)
            {
                if (display)
                    Error($"Failed to log method {method.Name} errored with the message {e.Message}");
            }

            if (display)
                Notify($"patching {method.Name} succeeded");
        }

        public static IEnumerable<MethodInfo> GetMethodsPatchingMethod(string name)
        {
            if (GetMethod(name, false, out MethodInfo method))
                return null;

            return GetMethodsPatchingMethod(method);
        }
        public static IEnumerable<MethodInfo> GetMethodsPatchingMethod(MethodInfo method)
        {
            Patches patches = Harmony.GetPatchInfo(method);

            foreach (Patch patch in patches.Prefixes) yield return patch.PatchMethod;
            foreach (Patch patch in patches.Postfixes) yield return patch.PatchMethod;
            foreach (Patch patch in patches.Transpilers) yield return patch.PatchMethod;
            foreach (Patch patch in patches.Finalizers) yield return patch.PatchMethod;
        }
        public static void PatchMethodPatches(string name, HarmonyMethod pre, HarmonyMethod post, bool display = true)
        {
            if (GetMethod(name, false, out MethodInfo method))
                return;

            PatchMethodPatches(method, pre, post, display);
        }
        public static void PatchMethodPatches(MethodInfo method, HarmonyMethod pre, HarmonyMethod post, bool display = true)
        {
            Patches patches = Harmony.GetPatchInfo(method);
            if (patches == null) return;

            foreach (Patch patch in patches.Prefixes)
            {
                PatchMethodFull(patch.PatchMethod, pre, post, display);
            }
            foreach (Patch patch in patches.Postfixes)
            {
                PatchMethodFull(patch.PatchMethod, pre, post, display);
            }
            foreach (Patch patch in patches.Transpilers)
            {
                PatchMethodFull(patch.PatchMethod, pre, post, display);
            }
        }

        /*
         * Method Unpatching
         */
        public static void UnpatchMethod(string name, bool display = true)
        {
            if (GetMethod(name, false, out MethodInfo method))
                return;

            UnpatchMethod(method);
        }
        public static void UnpatchMethod(MethodInfo method, bool display = true)
        {
            UnpatchMethodFull(method);
        }
        private static void UnpatchMethodFull(MethodInfo method, bool display = true)
        {
            foreach (MethodBase methodBase in Harmony.GetAllPatchedMethods())
            {
                Patches infos = Harmony.GetPatchInfo(methodBase);
                foreach (Patch infosPrefix in infos.Prefixes)
                {
                    if (infosPrefix.PatchMethod == method)
                    {
                        Modbase.harmony.Unpatch(methodBase, method);
                        return;
                    }
                }
                foreach (Patch infosPostfixesx in infos.Postfixes)
                {
                    if (infosPostfixesx.PatchMethod == method)
                    {
                        Modbase.harmony.Unpatch(methodBase, method);
                        return;
                    }
                }
            }
            if (display)
                Warn("Failed to locate method to unpatch");
        }
        public static void UnpatchMethodsOnMethod(string name)
        {
            if (GetMethod(name, false, out MethodInfo method))
                return;

            UnpatchMethodsOnMethod(method);
        }
        public static void UnpatchMethodsOnMethod(MethodInfo method)
        {
            UnpatchMethodsOnMethodFull(method);
        }
        private static void UnpatchMethodsOnMethodFull(MethodInfo method)
        {
            Modbase.harmony.Unpatch(method, HarmonyPatchType.All);
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
        public static void PatchType(string name, HarmonyMethod pre, HarmonyMethod post, bool display = true)
        {
            Type type = null;
            try
            {
                type = AccessTools.TypeByName(name);
            }
            catch (Exception e)
            {
                if (display)
                    Error($"Failed to locate type {name}, errored with the message {e.Message}");
                return;
            }

            PatchType(type, pre, post, display);
        }
        public static void PatchType(Type type, HarmonyMethod pre, HarmonyMethod post, bool display = true)
        {
            patchTypeThread = new Thread(() => PatchTypeFull(type, pre, post, display));
            patchTypeThread.Start();
        }
        private static void PatchTypeFull(Type type, HarmonyMethod pre, HarmonyMethod post, bool display = true)
        {
            try
            {
                if (PatchedTypes.Contains(type.FullName))
                {
                    if (display)
                        Warn($"patching {type.FullName} failed, already patched");
                    return;
                }
                PatchedTypes.Add(type.FullName);

                foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type))
                {
                    if (!PatchedMethods.Contains(method.Name) && method.DeclaringType == type && !method.IsSpecialName && !method.IsAssembly && method.HasMethodBody() && !method.IsGenericMethod)
                    {
                        try
                        {
                            byte[] bytes = method.GetMethodBody()?.GetILAsByteArray();
                            if (!(bytes?.Length == 0 || bytes?.Length == 1 && bytes.First() == 0x2A))
                            {
                                Modbase.harmony.Patch(method, pre, post);
                            }
                        }
                        catch (Exception e)
                        {
                            if (display)
                                Warn($"Failed to log method {method.Name} errored with the message {e.Message}");
                        }
                        PatchedMethods.Add(method.Name);
                    }
                }
                if (display)
                    Notify($"Patched {type.FullName}");
            }
            catch (Exception e)
            {
                if (display)
                    Error($"catch. patching {type.FullName} failed, {e.Message}");
            }
        }
        public static void PatchTypePatches(string name, HarmonyMethod pre, HarmonyMethod post, bool display = true)
        {
            Type type = null;
            try
            {
                type = AccessTools.TypeByName(name);
            }
            catch (Exception e)
            {
                if (display)
                    Error($"Failed to locate type {name}, errored with the message {e.Message}");
                return;
            }

            PatchTypePatchesFull(type, pre, post, display);
        }
        private static void PatchTypePatchesFull(Type type, HarmonyMethod pre, HarmonyMethod post, bool display = true)
        {
            try
            {
                foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type))
                {
                    Patches patches = Harmony.GetPatchInfo(method);
                    if (patches == null) continue;

                    foreach (Patch patch in patches.Prefixes)
                    {
                        PatchMethodFull(patch.PatchMethod, pre, post, display);
                    }
                    foreach (Patch patch in patches.Postfixes)
                    {
                        PatchMethodFull(patch.PatchMethod, pre, post, display);
                    }
                    foreach (Patch patch in patches.Transpilers)
                    {
                        PatchMethodFull(patch.PatchMethod, pre, post, display);
                    }
                }

                if (display)
                    Notify($"Sucessfully Patched the methods patching {type.FullName}");
            }
            catch (Exception e)
            {
                if (display)
                    Error($"patching {type.FullName} failed, {e.Message}");
            }
        }

        /*
         * Type Unpatching
         */

        public static void UnPatchTypePatches(string name, bool display = true)
        {
            Type type = null;
            try
            {
                type = AccessTools.TypeByName(name);
            }
            catch (Exception e)
            {
                if (display)
                    Error($"Failed to locate type {name}, errored with the message {e.Message}");
                return;
            }

            UnPatchTypePatches(type, display);
        }
        public static void UnPatchTypePatches(Type type, bool display = true)
        {
            if (type == null)
            {
                if (display)
                    Error("Cannnot unpatch null");

                return;
            }

            UnPatchTypePatchesFull(type, display);
        }
        private static void UnPatchTypePatchesFull(Type type, bool display = true)
        {
            try
            {
                foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type))
                {
                    UnpatchMethodsOnMethod(method);
                }

                if (display)
                    Notify($"Sucessfully unpatched the methods patching {type.FullName}");
            }
            catch (Exception e)
            {
                if (display)
                    Error($"unpatching {type.FullName} failed, {e.Message}");
            }
        }

        /*
         * Internal Method Patching
         */

        public static void PatchInternalMethod(string name, bool display = true)
        {
            MethodInfo method = null;
            try
            {
                method = AccessTools.Method(name);
            }
            catch (Exception e)
            {
                if (display)
                    Error($"Failed to locate method {name}, errored with the message {e.Message}");
                return;
            }
            PatchInternalMethod(method, display);
        }
        public static void PatchInternalMethod(MethodInfo method, bool display = true)
        {
            if (InternalMethodUtility.PatchedInternals.Contains(method))
            {
                if (display)
                    Warn("Trying to re-transpile an already profiled internal method");
                return;
            }
            PatchInternalMethodFull(method, display);
        }
        private static void PatchInternalMethodFull(MethodInfo method, bool display = true)
        {
            try
            {
                AnalyzerState.MakeAndSwitchTab(method.Name + "-int");

                InternalMethodUtility.curMeth = method;
                InternalMethodUtility.PatchedInternals.Add(method);
                InternalMethodUtility.Harmony.Patch(method, null, null, InternalMethodUtility.InternalProfiler);
            }
            catch (Exception e)
            {
                if (display)
                    Error("Failed to patch internal methods, failed with the error " + e.Message);
                InternalMethodUtility.PatchedInternals.Remove(method);
            }
        }

        public static void UnpatchInternalMethod(string name, bool display = true)
        {
            MethodInfo method = null;
            try
            {
                method = AccessTools.Method(name);
            }
            catch (Exception e)
            {
                if (display)
                    Error($"Failed to locate method {name}, errored with the message {e.Message}");
                return;
            }
            UnpatchInternalMethod(method, display);
        }
        public static void UnpatchInternalMethod(MethodInfo method, bool display = true)
        {
            if (!InternalMethodUtility.PatchedInternals.Contains(method))
            {
                if (display)
                    Warn($"There is no method with the name {method.Name} that has been noted as profiled");
                return;
            }
            UnpatchInternalMethodFull(method, display);
        }
        private static void UnpatchInternalMethodFull(MethodInfo method, bool display = true)
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
            InternalMethodUtility.curMeth = null;
        }

        public static void PatchAssembly(string name, bool display = true)
        {
            ModContentPack mod = LoadedModManager.RunningMods.FirstOrDefault(m => m.Name == name || m.PackageId == name.ToLower());

            if (display)
                Notify($"Mod is null? {mod == null}");
            if (mod != null)
            {
                if (display)
                    Notify($"Assembly count: { mod.assemblies?.loadedAssemblies?.Count ?? 0}");
                foreach (Assembly ass in mod.assemblies?.loadedAssemblies)
                {
                    if (display)
                        Notify($"Assembly named: {ass.FullName}, located at {ass.Location}");
                }
            }

            IEnumerable<Assembly> assembly = mod?.assemblies?.loadedAssemblies?.Where(w => !w.FullName.Contains("Harmony") && !w.FullName.Contains("0MultiplayerAPI"));

            if (assembly != null && assembly.Count() != 0)
            {
                AnalyzerState.MakeAndSwitchTab(mod.Name + "-prof");
                HarmonyMethod pre = new HarmonyMethod(AccessTools.TypeByName(mod.Name + "-prof").GetMethod("Prefix", BindingFlags.Public | BindingFlags.Static));
                HarmonyMethod post = new HarmonyMethod(AccessTools.TypeByName(mod.Name + "-prof").GetMethod("Postfix", BindingFlags.Public | BindingFlags.Static));

                patchAssemblyThread = new Thread(() => PatchAssemblyFull(assembly.ToList(), pre, post, display));
                patchAssemblyThread.Start();
            }
            else
            {
                if (display)
                    Error($"Failed to patch {name}");
            }
        }
        private static void PatchAssemblyFull(List<Assembly> assemblies, HarmonyMethod pre, HarmonyMethod post, bool display = true)
        {
            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    if (PatchedAssemblies.Contains(assembly.FullName))
                    {
                        if (display)
                            Warn($"patching {assembly.FullName} failed, already patched");
                        return;
                    }
                    PatchedAssemblies.Add(assembly.FullName);

                    foreach (Type type in assembly.GetTypes())
                    {
                        if (type.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() == null)
                            PatchTypeFull(type, pre, post, display);
                    }

                    if (display)
                        Notify($"Patched {assembly.FullName}");
                }
                catch (Exception e)
                {
                    if (display)
                        Error($"catch. patching {assembly.FullName} failed, {e.Message}");
                }
            }
        }

    }
}
