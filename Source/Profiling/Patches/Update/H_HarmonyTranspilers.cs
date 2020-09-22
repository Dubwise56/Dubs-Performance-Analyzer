using HarmonyLib;
using RimWorld.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    [Entry("HarmonyTranspilers", Category.Update, "TransPatchTipKey")]
    public static class H_HarmonyTranspilers
    {
        public static bool Active = false;

        public static void ProfilePatch()
        {
            var patches = Harmony.GetAllPatchedMethods().ToList();

            var filteredTranspilers = patches
                .Where(m => Harmony.GetPatchInfo(m).Transpilers.Any(p => Utility.IsNotAnalyzerPatch(p.owner) && !TranspilerMethodUtility.PatchedMeths.Contains(m)))
                .ToList();

            TranspilerMethodUtility.PatchedMeths.AddRange(filteredTranspilers);

            foreach (var meth in filteredTranspilers)
            {
                try
                {
                    Modbase.Harmony.Patch(meth, transpiler: TranspilerMethodUtility.TranspilerProfiler);
                }
                catch (Exception e)
                {
#if DEBUG
                        ThreadSafeLogger.Error($"[Analyzer] Failed to patch transpiler, failed with the message {e.Message}");
#endif
#if NDEBUG
                    if (Settings.verboseLogging)
                        ThreadSafeLogger.Error($"[Analyzer] Failed to patch transpiler, failed with the message {e.Message}");
#endif
                }
            }
        }
    }
}




