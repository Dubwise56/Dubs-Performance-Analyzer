using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DubsAnalyzer
{
    [ProfileMode("Custom Tick", UpdateMode.Tick)]
    class CustomProfilersTick
    {
        public static bool Active = false;
        public static void PatchMeth(string strde)
        {
            var listStrLineElements = strde.Split(',').ToList();

            foreach (var str in listStrLineElements)
            {
                try
                {
                    var sav = AccessTools.Method(str);
                    if (sav != null)
                    {
                        Messages.Message($"Patched {str}", MessageTypeDefOf.TaskCompletion, false);
                        Analyzer.harmony.Patch(sav, new HarmonyMethod(typeof(CustomProfilersTick), nameof(Prefix)),
                            new HarmonyMethod(typeof(CustomProfilersTick), nameof(Postfix)));
                    }
                    else
                    {
                        Messages.Message($"{str} not found", MessageTypeDefOf.NegativeEvent, false);
                    }
                }
                catch (Exception)
                {
                    Messages.Message($"catch. {str} failed", MessageTypeDefOf.NegativeEvent, false);
                }
            }
        }

        public static void Prefix(object __instance, MethodBase __originalMethod, ref string __state)
        {
            if (!Active)
            {
                return;
            }
            __state = string.Empty;
            if (__instance != null)
            {
                __state = $"{__instance.GetType().Name}.{__originalMethod.Name}";
            }
            else
            if (__originalMethod.ReflectedType != null)
            {
                __state = $"{__originalMethod.ReflectedType.Name}.{__originalMethod.Name}";
            }

            Analyzer.Start(__state);
        }

        public static void Postfix(string __state)
        {
            if (Active)
            {
                Analyzer.Stop(__state);
            }
        }
    }

    [ProfileMode("Custom Update", UpdateMode.Update)]
    class CustomProfilersUpdate
    {
        public static bool Active = false;
        public static List<string> PatchedAssemblies = new List<string>();
        public static void PatchMeth(string strde)
        {
            if (strde.First() == '@')
            {
                PatchAssembly(strde.Substring(1, strde.Length - 1));
                return;
            }

            var listStrLineElements = strde.Split(',').ToList();
            foreach (var str in listStrLineElements)
            {
                try
                {
                    var sav = AccessTools.Method(str);
                    if (sav != null)
                    {
                        Messages.Message($"Patched {str}", MessageTypeDefOf.TaskCompletion, false);
                        Analyzer.harmony.Patch(sav, new HarmonyMethod(typeof(CustomProfilersUpdate), nameof(Prefix)),
                            new HarmonyMethod(typeof(CustomProfilersUpdate), nameof(Postfix)));
                    }
                    else
                    {
                        Messages.Message($"{str} not found", MessageTypeDefOf.NegativeEvent, false);
                    }
                }
                catch (Exception)
                {
                    Messages.Message($"catch. {str} failed", MessageTypeDefOf.NegativeEvent, false);
                }
            }
        }

        public static void PatchAssembly(string AssemblyName)
        {
            Log.Warning("WIP Functionality");
            return;

            // WIP FUNCTIONALITY - CRASHES GAME WITH BIG ASSEMBLIES CURRENTLY

#pragma warning disable CS0162 // Unreachable code detected
            if (PatchedAssemblies.Contains(AssemblyName))
#pragma warning restore CS0162 // Unreachable code detected
            {
                Messages.Message($"patching {AssemblyName} failed, already patched", MessageTypeDefOf.NegativeEvent, false);
                return;
            }
            Mod mod = LoadedModManager.ModHandles.FirstOrDefault(m => m.Content.Name == AssemblyName);
            Assembly assembly = mod.Content.assemblies.loadedAssemblies.First();

            if (assembly != null)
            {
                try
                {
                    PatchedAssemblies.Add(AssemblyName);
                    foreach (var type in assembly.DefinedTypes)
                    {
                        foreach (var method in AccessTools.GetDeclaredMethods(type))
                        {
                            try
                            {
                                Analyzer.harmony.Patch(method,
                                    new HarmonyMethod(typeof(CustomProfilersUpdate), nameof(Prefix)),
                                    new HarmonyMethod(typeof(CustomProfilersUpdate), nameof(Postfix))
                                );
                            } catch (Exception e) { Log.Warning($"Failed to log method {method.Name} erroed with the message {e.Message}"); }
                        }
                    }
                    Messages.Message($"Patched {AssemblyName}", MessageTypeDefOf.TaskCompletion, false);
                } catch (Exception e)
                {
                    Messages.Message($"catch. patching {AssemblyName} failed, {e.Message}", MessageTypeDefOf.NegativeEvent, false);
                }
            }
        }

        public static void Prefix(object __instance, MethodBase __originalMethod, ref string __state)
        {
            
            if (!Active)
            {
                return;
            }
            __state = string.Empty;
            if (__instance != null)
            {
                __state = $"{__instance.GetType().Name}.{__originalMethod.Name}";
            }
            else
            if (__originalMethod.ReflectedType != null)
            {
                __state = $"{__originalMethod.ReflectedType.Name}.{__originalMethod.Name}";
            }

            Analyzer.Start(__state);
        }

        public static void Postfix(string __state)
        {
            if (Active)
            {
                Analyzer.Stop(__state);
            }
        }
    }
}
