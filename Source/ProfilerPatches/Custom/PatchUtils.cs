using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DubsAnalyzer
{
    public static class PatchUtils
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
                listStrLineElements.AddRange(name.Split(','));
            if (name.Contains(';'))
                listStrLineElements.AddRange(name.Split(';'));
            else if (name.Contains('.'))
                listStrLineElements.Add(name.Replace('.', ':'));
            else
                listStrLineElements.Add(name);

            return listStrLineElements;
        }

        private static void Notify(string message)
        {
            Dialog_Analyzer.QueuedMessages.Add(delegate { Messages.Message(message, MessageTypeDefOf.PositiveEvent, false); });
        }
        private static void Warn(string message)
        {
            Dialog_Analyzer.QueuedMessages.Add(delegate { Messages.Message(message, MessageTypeDefOf.CautionInput, false); });
        }
        private static void Error(string message)
        {
            Dialog_Analyzer.QueuedMessages.Add(delegate { Messages.Message(message, MessageTypeDefOf.NegativeEvent, false); });
        }

        /*
         * Method Patching
         */
        public static void PatchMethod(string name, HarmonyMethod pre, HarmonyMethod post)
        {
            MethodInfo method = null;
            try
            {
                method = AccessTools.Method(name);
            }
            catch (Exception e)
            {
                Error($"failed to locate method {name}, errored with the message {e.Message}");
                return;
            }
            PatchMethod(method, pre, post);
        }
        public static void PatchMethod(MethodInfo method, HarmonyMethod pre, HarmonyMethod post)
        {
            PatchMethodFull(method, pre, post);
        }
        private static void PatchMethodFull(MethodInfo method, HarmonyMethod pre, HarmonyMethod post)
        {
            if (PatchedMethods.Contains(method.Name))
            {
                Warn($"patching {method.Name} failed, already patched");
                return;
            }

            PatchedMethods.Add(method.Name);
            try
            {
                Analyzer.harmony.Patch(method, pre, post);
            }
            catch (Exception e) { Error($"Failed to log method {method.Name} errored with the message {e.Message}"); }

            Notify($"patching {method.Name} succeeded");
        }
        public static void PatchMethodPatches(string name, HarmonyMethod pre, HarmonyMethod post)
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
            PatchMethodPatches(method, pre, post);
        }
        public static void PatchMethodPatches(MethodInfo method, HarmonyMethod pre, HarmonyMethod post)
        {
            Patches patches = Harmony.GetPatchInfo(method);
            if (patches == null) return;

            foreach (Patch patch in patches.Prefixes)
            {
                PatchMethodFull(patch.PatchMethod, pre, post);
            }
            foreach (Patch patch in patches.Postfixes)
            {
                PatchMethodFull(patch.PatchMethod, pre, post);
            }
            foreach (Patch patch in patches.Transpilers)
            {
                PatchMethodFull(patch.PatchMethod, pre, post);
            }
        }

        /*
         * Method Unpatching
         */
        public static void UnpatchMethod(string name)
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
            UnpatchMethod(method);
        }
        public static void UnpatchMethod(MethodInfo method)
        {
            UnpatchMethodFull(method);
        }
        private static void UnpatchMethodFull(MethodInfo method)
        {
            foreach (var methodBase in Harmony.GetAllPatchedMethods())
            {
                var infos = Harmony.GetPatchInfo(methodBase);
                foreach (var infosPrefix in infos.Prefixes)
                {
                    if (infosPrefix.PatchMethod == method)
                    {
                        Analyzer.harmony.Unpatch(methodBase, method);
                        return;
                    }
                }
                foreach (var infosPostfixesx in infos.Postfixes)
                {
                    if (infosPostfixesx.PatchMethod == method)
                    {
                        Analyzer.harmony.Unpatch(methodBase, method);
                        return;
                    }
                }
            }
            Warn("Failed to locate method to unpatch");
        }
        public static void UnpatchMethodsOnMethod(string name)
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
            UnpatchMethodsOnMethod(method);
        }
        public static void UnpatchMethodsOnMethod(MethodInfo method)
        {
            UnpatchMethodsOnMethodFull(method);
        }
        private static void UnpatchMethodsOnMethodFull(MethodInfo method)
        {
            Analyzer.harmony.Unpatch(method, HarmonyPatchType.All, "*");
        }

        /*
         * Type Patching
         */

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
                if (PatchedTypes.Contains(type.FullName))
                {
                    Warn($"patching {type.FullName} failed, already patched");
                    return;
                }
                PatchedTypes.Add(type.FullName);

                foreach (var method in AccessTools.GetDeclaredMethods(type))
                {
                    if (!PatchedMethods.Contains(method.Name) && method.DeclaringType == type && !method.IsSpecialName && !method.IsAssembly)
                    {
                        try
                        {
                            Analyzer.harmony.Patch(method, pre, post);
                        }
                        catch (Exception e) { Log.Warning($"Failed to log method {method.Name} errored with the message {e.Message}"); }
                        PatchedMethods.Add(method.Name);
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
                foreach (var method in AccessTools.GetDeclaredMethods(type))
                {
                    Patches patches = Harmony.GetPatchInfo(method);
                    if (patches == null) continue;

                    foreach (Patch patch in patches.Prefixes)
                    {
                        PatchMethodFull(patch.PatchMethod, pre, post);
                    }
                    foreach (Patch patch in patches.Postfixes)
                    {
                        PatchMethodFull(patch.PatchMethod, pre, post);
                    }
                    foreach (Patch patch in patches.Transpilers)
                    {
                        PatchMethodFull(patch.PatchMethod, pre, post);
                    }
                }

                Notify($"Sucessfully Patched the methods patching {type.FullName}");
            }
            catch (Exception e)
            {
                Error($"patching {type.FullName} failed, {e.Message}");
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
        }
        public static void PatchInternalMethod(MethodInfo method)
        {
            if (InternalMethodUtility.PatchedInternals.ContainsKey(method))
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
                InternalMethodUtility.curMeth = method;
                InternalMethodUtility.PatchedInternals.Add(method, null);
                InternalMethodUtility.Harmony.Patch(method, null, null, InternalMethodUtility.InternalProfiler);
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
            if(!InternalMethodUtility.PatchedInternals.ContainsKey(method))
            {
                Warn($"There is no method with the name {method.Name} that has been noted as profiled");
                return;
            }
            UnpatchInternalMethodFull(method);
        }
        private static void UnpatchInternalMethodFull(MethodInfo method)
        {
            InternalMethodUtility.curMeth = method;
            InternalMethodUtility.Harmony.Patch(method, null, null, InternalMethodUtility.UnProfiler);
            InternalMethodUtility.PatchedInternals.Remove(method);
        }

        public static void UnpatchAllInternalMethods()
        {
            UnpatchAllInternalMethodsFull();
        }
        private static void UnpatchAllInternalMethodsFull()
        {
            foreach(var meth in InternalMethodUtility.PatchedInternals.Keys.ToList())
            {
                InternalMethodUtility.curMeth = meth;
                InternalMethodUtility.Harmony.Patch(meth, null, null, InternalMethodUtility.UnProfiler);
            }

            InternalMethodUtility.PatchedInternals.Clear();
            InternalMethodUtility.curMeth = null;
        }


        /*
         * WIP
         */
        public static void PatchAssembly(string name, HarmonyMethod pre, HarmonyMethod post)
        {
            Mod mod = LoadedModManager.ModHandles.FirstOrDefault(m => m.Content.Name == name);
            Assembly assembly = mod.Content?.assemblies?.loadedAssemblies?.First();

            if (assembly != null)
            {
                patchAssemblyThread = new Thread(() => PatchAssemblyFull(assembly, pre, post));
                patchAssemblyThread.Start();
            }
            else
            {
                Messages.Message($"Failed to patch {name}", MessageTypeDefOf.NegativeEvent, false);
            }
        }
        private static void PatchAssemblyFull(Assembly assembly, HarmonyMethod pre, HarmonyMethod post)
        {
            try
            {
                if (PatchedAssemblies.Contains(assembly.FullName))
                {
                    Messages.Message($"patching {assembly.FullName} failed, already patched", MessageTypeDefOf.NegativeEvent, false);
                    return;
                }
                PatchedAssemblies.Add(assembly.FullName);

                foreach (var type in assembly.DefinedTypes)
                {
                    PatchTypeFull(type, pre, post);
                }

                Messages.Message($"Patched {assembly.FullName}", MessageTypeDefOf.TaskCompletion, false);
            }
            catch (Exception e)
            {
                Messages.Message($"catch. patching {assembly.FullName} failed, {e.Message}", MessageTypeDefOf.NegativeEvent, false);
            }
        }

    }
}
