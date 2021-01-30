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

	        var filteredTranspilers = patches.Where(m => Harmony.GetPatchInfo(m).Transpilers.Any(p => Utility.IsNotAnalyzerPatch(p.owner))).ToList();

	        foreach (var filteredTranspiler in filteredTranspilers)
	        {
		        yield return filteredTranspiler as MethodInfo;
		        //  Modbase.Harmony.Patch(AccessTools.Method(typeof(DynamicDrawManager), nameof(DynamicDrawManager.DrawDynamicThings)), prefix: new HarmonyMethod(typeof(H_DrawDynamicThings), "Prefix"));
	        }

           // yield return AccessTools.Method(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt), new Type[] { typeof(Vector3), typeof(RotDrawMode), typeof(bool), typeof(bool) });
        }
    }
}




