using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace DubsAnalyzer
{
    [ProfileMode("HarmonyTranspilers", UpdateMode.Update, "Profiles any methods that have been modified using harmony transpiler, this is the total time for the whole method, not just the parts that were patched in")]
    internal class H_HarmonyTranspilers
    {
        public static bool Active = false;

        public static void ProfilePatch()
        {
            var go = new HarmonyMethod(typeof(H_HarmonyTranspilers), nameof(Prefix));
            var biff = new HarmonyMethod(typeof(H_HarmonyTranspilers), nameof(Postfix));
            int c = 0;
            int p = 0;
            foreach (var mode in Analyzer.harmony.GetPatchedMethods().ToList())
            {
                c++;

                Patches patchInfo =Harmony.GetPatchInfo(mode);
                var pilers = patchInfo.Transpilers;
                if (!pilers.NullOrEmpty())
                {
                    p++;
                    bool F = pilers.Any(x => x.owner != Analyzer.harmony.Id);

                    if (F)
                    {
                        Analyzer.harmony.Patch(mode, go, biff);
                    }
                }
            }

            Log.Warning($"{c} patched methods");
            Log.Warning($"{p} with transpilers");
        }

        public static void Prefix(MethodBase __originalMethod, ref string __state)
        {
            if (!Active)
            {
                return;
            }

            __state = __originalMethod.Name;

            Analyzer.Start(__state, () =>
            {
                if (__originalMethod.ReflectedType != null)
                {
                    return $"{__originalMethod.Name} {__originalMethod.ReflectedType.FullName}";
                }
                return $"{__originalMethod.Name} {__originalMethod.GetType().FullName}";
            });
        }

        public static void Postfix(string __state)
        {
            if (Active)
            {
                Analyzer.Stop(__state);
            }
        }
    }

    [ProfileMode("HarmonyPatches", UpdateMode.Update, "Tries to profile how long prefixes and postfixes from harmony patches take to execute, does not include code from transpilers")]
    internal class H_HarmonyPatches
    {
        public static bool Active = false;

        public static void ProfilePatch()
        {
            var go = new HarmonyMethod(typeof(H_HarmonyPatches), nameof(Prefix));
            var biff = new HarmonyMethod(typeof(H_HarmonyPatches), nameof(Postfix));

            foreach (var mode in Analyzer.harmony.GetPatchedMethods().ToList())
            {
                Patches patchInfo = Harmony.GetPatchInfo(mode);
                foreach (var fix in patchInfo.Prefixes)
                {
                    if (Analyzer.harmony.Id != fix.owner)
                    {
                        //  Log.Warning($"Logging prefix on {mode.Name} by {fix.owner}");
                        Analyzer.harmony.Patch(fix.PatchMethod, go, biff);
                    }
                }
                foreach (var fix in patchInfo.Postfixes)
                {
                    if (Analyzer.harmony.Id != fix.owner)
                    {
                        //   Log.Warning($"Logging postfix on {mode.Name} by {fix.owner}");
                        Analyzer.harmony.Patch(fix.PatchMethod, go, biff);
                    }
                }

            }
        }

        public static void Prefix(MethodBase __originalMethod, ref string __state)
        {
            if (!Active)
            {
                return;
            }
           
            __state =  __originalMethod.GetHashCode().ToString();

            Analyzer.Start(__state, () =>
            {
                if (__originalMethod.ReflectedType != null)
                {
                    return $"{__originalMethod.ToString()} {__originalMethod.ReflectedType.FullName}";
                }
                return $"{__originalMethod.ToString()} {__originalMethod.GetType().FullName}";
            });
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