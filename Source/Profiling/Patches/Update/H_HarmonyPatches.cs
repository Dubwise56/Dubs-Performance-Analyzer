﻿using HarmonyLib;
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
    [Entry("entry.update.harmonypatches", Category.Update, "entry.update.harmonypatches.tooltip")]
    internal class H_HarmonyPatches
    {
        public static bool Active = false;

        public static IEnumerable<MethodInfo> GetPatchMethods()
        {
            foreach (MethodBase mode in Harmony.GetAllPatchedMethods())
            {
                Patches patchInfo = Harmony.GetPatchInfo(mode);
                foreach (var fix in patchInfo.Prefixes.Concat(patchInfo.Postfixes).Where(f => Utility.IsNotAnalyzerPatch(f.owner)))
                {
                    yield return fix.PatchMethod;
                }
            }
        }

        public static void ProfilePatch()
        {
            try
            {
                MethodTransplanting.PatchMethods(typeof(H_HarmonyPatches));
            }
            catch
            {
            }
        }
    }
}