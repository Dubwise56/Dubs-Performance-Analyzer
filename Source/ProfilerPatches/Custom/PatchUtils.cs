using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public static IEnumerable<string> GetSplitString(string name)
        {
            List<string> listStrLineElements = new List<string>();

            if (name.Contains(','))
                listStrLineElements.AddRange(name.Split(','));
            if (name.Contains(';'))
                listStrLineElements.AddRange(name.Split(';'));
            else
                listStrLineElements.Add(name);
            
            return listStrLineElements;
        }

        public static void PatchAssembly(string name, HarmonyMethod pre, HarmonyMethod post)
        {
            Mod mod = LoadedModManager.ModHandles.FirstOrDefault(m => m.Content.Name == name);
            Assembly assembly = mod.Content.assemblies.loadedAssemblies.First();

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
                    foreach (var method in AccessTools.GetDeclaredMethods(type))
                    {
                        if (method.DeclaringType == type && !method.IsSpecialName && !method.IsAssembly)
                        {
                            try
                            {
                                Analyzer.harmony.Patch(method, pre, post);
                            }
                            catch (Exception e) { Log.Warning($"Failed to log method {method.Name} errored with the message {e.Message}"); }

                            PatchedMethods.Add(method.Name);
                        }
                    }
                    PatchedTypes.Add(type.FullName);
                }
                Messages.Message($"Patched {assembly.FullName}", MessageTypeDefOf.TaskCompletion, false);
            }
            catch (Exception e)
            {
                Messages.Message($"catch. patching {assembly.FullName} failed, {e.Message}", MessageTypeDefOf.NegativeEvent, false);
            }
        }
        
        public static void PatchType(string name, HarmonyMethod pre, HarmonyMethod post)
        {
            Type type = AccessTools.TypeByName(name);

            if (type != null)
            {
                patchTypeThread = new Thread(() => PatchTypeFull(type, pre, post));
                patchTypeThread.Start();
            }
            else
            {
                Messages.Message($"Failed to patch {name}", MessageTypeDefOf.NegativeEvent, false);
            }
        }
        private static void PatchTypeFull(Type type, HarmonyMethod pre, HarmonyMethod post)
        {
            try
            {
                if (PatchedTypes.Contains(type.FullName))
                {
                    Messages.Message($"patching {type.FullName} failed, already patched", MessageTypeDefOf.NegativeEvent, false);
                    return;
                }

                PatchedTypes.Add(type.FullName);

                foreach (var method in AccessTools.GetDeclaredMethods(type))
                {
                    if (method.DeclaringType == type && !method.IsSpecialName && !method.IsAssembly)
                    {
                        try
                        {
                            Analyzer.harmony.Patch(method, pre, post);
                        }
                        catch (Exception e) { Log.Warning($"Failed to log method {method.Name} errored with the message {e.Message}"); }
                    }
                    PatchedMethods.Add(method.Name);
                }
                Messages.Message($"Patched {type.FullName}", MessageTypeDefOf.TaskCompletion, false);
            }
            catch (Exception e)
            {
                Messages.Message($"catch. patching {type.FullName} failed, {e.Message}", MessageTypeDefOf.NegativeEvent, false);
            }
        }
       
        public static void PatchMethod(string name, HarmonyMethod pre, HarmonyMethod post)
        {
            MethodInfo method = AccessTools.Method(name);

            if (method != null)
            {
                PatchMethodFull(method, pre, post);
            }
            else
            {
                Messages.Message($"Failed to patch {name}", MessageTypeDefOf.NegativeEvent, false);
            }
        }
        private static void PatchMethodFull(MethodInfo method, HarmonyMethod pre, HarmonyMethod post)
        {
            if (PatchedMethods.Contains(method.Name))
            {
                Messages.Message($"patching {method.Name} failed, already patched", MessageTypeDefOf.NegativeEvent, false);
                return;
            }

            PatchedTypes.Add(method.Name);
            try
            {
                Analyzer.harmony.Patch(method, pre, post);
            }
            catch (Exception e) { Log.Warning($"Failed to log method {method.Name} errored with the message {e.Message}"); }

            Messages.Message($"Patched {method.Name}", MessageTypeDefOf.TaskCompletion, false);
        }

        public static void PatchMethodPatches(string name, HarmonyMethod pre, HarmonyMethod post)
        {
            MethodInfo method = AccessTools.Method(name);

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

        //public static void PatchInternalMethods(MethodInfo name, HarmonyMethod pre, HarmonyMethod post)
        //{

        //    return name.GetMethodBody().GetILAsByteArray()  .Body.Instructions
        //    .Where(x => x.OpCode == OpCodes.Call)
        //    .Select(x => (MethodDefinition)x.Operand);
        //}

    }
}
