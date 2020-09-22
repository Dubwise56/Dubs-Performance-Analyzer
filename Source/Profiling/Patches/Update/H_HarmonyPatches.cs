using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{

    [Entry("HarmonyPatches", Category.Update, "HarmPatchesTipKey")]
    internal class H_HarmonyPatches
    {
        public static bool Active = false;

        public static IEnumerable<MethodInfo> GetPatchMethods()
        {
            List<MethodBase> patches = Harmony.GetAllPatchedMethods().ToList();

            foreach (MethodBase mode in patches)
            {
                Patches patchInfo = Harmony.GetPatchInfo(mode);
                foreach (Patch fix in patchInfo.Prefixes)
                {
                    if (Utility.IsNotAnalyzerPatch(fix.owner) && !PatchedPres.Contains(fix))
                    {
                        PatchedPres.Add(fix);
                        yield return fix.PatchMethod;
                    }
                }

                foreach (Patch fix in patchInfo.Postfixes)
                {

                    if (Utility.IsNotAnalyzerPatch(fix.owner) && !PatchedPosts.Contains(fix))
                    {
                        PatchedPosts.Add(fix);
                        yield return fix.PatchMethod;
                    }
                }
            }
        }

        public static List<Patch> PatchedPres = new List<Patch>();
        public static List<Patch> PatchedPosts = new List<Patch>();
        public static void ProfilePatch()
        {
            try
            {
                MethodTransplanting.PatchMethods(typeof(H_HarmonyPatches));
            }
            catch { }
        }
    }
}