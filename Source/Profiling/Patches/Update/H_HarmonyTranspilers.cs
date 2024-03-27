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
    [Entry("entry.update.transpilermethods", Category.Update)]
    public static class H_HarmonyTranspiledMethods
    {
        public static bool Active = false;
        
        public static IEnumerable<MethodInfo> GetPatchMethods()
        {
            var patches = Harmony.GetAllPatchedMethods().ToList();

            foreach (var patch in patches)
            {
                HarmonyLib.Patches p = null;
                try
                {
                    p = Harmony.GetPatchInfo(patch);
                }
                catch (Exception e)
                {
                    ThreadSafeLogger.ReportException(e, $"Failed to get patch info for {Utility.GetSignature(patch)}");
                    continue;
                }
                
                if(patch is MethodInfo info && p.Transpilers.Any(p => Utility.IsNotAnalyzerPatch(p.owner))) 
                    yield return info;
            }

        }
    }
}




