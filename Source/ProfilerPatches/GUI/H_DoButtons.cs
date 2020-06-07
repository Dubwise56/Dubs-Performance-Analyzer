using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DubsAnalyzer
{
    //[ProfileMode("DoButtons", UpdateMode.GUI, "The main tab buttons along the bottom of the screen")]
    //internal class H_DoButtons
    //{
    //    public static bool Active = false;

    //    public static void ProfilePatch()
    //    {
    //        foreach (var mainButtonDef in DefDatabase<MainButtonDef>.AllDefs)
    //        {
    //            var org = mainButtonDef.workerClass.GetMethod("DoButton");
    //            var pre = new HarmonyMethod(typeof(H_DoButtons), nameof(Start));
    //            var post = new HarmonyMethod(typeof(H_DoButtons), nameof(Stop));

    //            var bo = false;
    //            var info = Harmony.GetPatchInfo(org);
    //            if (info != null)
    //            {
    //                foreach (var prefix in info.Prefixes)
    //                {
    //                    if (prefix.PatchMethod == pre.method)
    //                    {
    //                        bo = true;
    //                    }
    //                }
    //            }

    //            if (bo == false)
    //            {
    //                Analyzer.harmony.Patch(org, pre, post);
    //            }
    //        }
    //    }

    //    public static void Start(MainButtonWorker __instance, ref string __state)
    //    {
    //        if (Active)
    //        {
    //            __state = __instance.def.defName; 
    //            Analyzer.Start(__state);
    //        }
    //    }

    //    public static void Stop(string __state)
    //    {
    //        if (Active)
    //        {
    //            Analyzer.Stop(__state);
    //        }
    //    }
    //}
}