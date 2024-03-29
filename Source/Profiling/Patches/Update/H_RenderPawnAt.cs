﻿using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    [Entry("entry.update.pawnrenderer", Category.Update)]
    internal class H_RenderPawnAt
    {
        public static bool Active = false;

        public static IEnumerable<MethodInfo> GetPatchMethods()
        {
            //foreach (var method in Utility.GetTypeMethods(typeof(PawnRenderer))
            //    yield return method;

            yield return AccessTools.Method(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt));
        }
        public static string GetLabel(PawnRenderer __instance) => $"{__instance.pawn.Label} - {__instance.pawn.ThingID}";
        public static string GetName(PawnRenderer __instance) => __instance.pawn.GetHashCode().ToString();
    }
}