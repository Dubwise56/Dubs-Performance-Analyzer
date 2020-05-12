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
        private static Thread patchAssemblyThread = null;
        private static Thread patchTypeThread = null;

        public static void PatchAssembly(string name)
        {
            Mod mod = LoadedModManager.ModHandles.FirstOrDefault(m => m.Content.Name == name);
            Assembly assembly = mod.Content.assemblies.loadedAssemblies.First();

            if (assembly != null)
            {
                patchAssemblyThread = new Thread(() => PatchAssemblyFull(assembly));
                patchAssemblyThread.Start();
            }
            else
            {
                Messages.Message($"Failed to patch {name}", MessageTypeDefOf.NegativeEvent, false);
            }
        }

        private static void PatchAssemblyFull(Assembly assembly)
        {
            try
            {
                if(PatchedAssemblies.Contains(assembly.FullName))
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
                                Analyzer.harmony.Patch(method,
                                    new HarmonyMethod(typeof(CustomProfilersUpdate), nameof(CustomProfilersUpdate.Prefix)),
                                    new HarmonyMethod(typeof(CustomProfilersUpdate), nameof(CustomProfilersUpdate.Postfix))
                                );
                            }
                            catch (Exception e) { Log.Warning($"Failed to log method {method.Name} erroed with the message {e.Message}"); }
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

        public static void PatchType(string name)
        {
            Type type = AccessTools.TypeByName(name);

            if (type != null)
            {
                patchTypeThread = new Thread(() => PatchTypeFull(type));
                patchTypeThread.Start();
            }
            else
            {
                Messages.Message($"Failed to patch {name}", MessageTypeDefOf.NegativeEvent, false);
            }
        }

        public static void PatchTypeFull(Type type)
        {
            try
            {
                if(PatchedTypes.Contains(type.FullName))
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
                            Analyzer.harmony.Patch(method,
                                new HarmonyMethod(typeof(CustomProfilersUpdate), nameof(CustomProfilersUpdate.Prefix)),
                                new HarmonyMethod(typeof(CustomProfilersUpdate), nameof(CustomProfilersUpdate.Postfix))
                            );
                        }
                        catch (Exception e) { Log.Warning($"Failed to log method {method.Name} erroed with the message {e.Message}"); }
                    }
                }
                Messages.Message($"Patched {type.FullName}", MessageTypeDefOf.TaskCompletion, false);
            }
            catch (Exception e)
            {
                Messages.Message($"catch. patching {type.FullName} failed, {e.Message}", MessageTypeDefOf.NegativeEvent, false);
            }
        }
    }
}
