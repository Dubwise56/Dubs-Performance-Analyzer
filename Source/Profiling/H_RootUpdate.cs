using HarmonyLib;
using System;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    public class H_RootUpdate
    {
        public static void Prefix()
        {
            ProfileController.phase = ProfileController.PhaseUpdate;
            ProfileController.gate = ProfileController.GateFor(GUIController.CurrentCategory);
            Profiler.ResetStack();

            if (GUIController.CurrentCategory != Category.Tick)
                ProfileController.BeginUpdate();
        }

        public static void Postfix()
        {
            if (GUIController.CurrentCategory != Category.Tick) // If we are tick, we will 'update' in the TickManager.DoSingleTick method
                ProfileController.EndUpdate();

#if V1_7
            VanillaProfilerMirror.EndFrame();
#endif

            ProfileController.phase = 0;
        }

        public static void GUIPrefix()
        {
            ProfileController.phase = ProfileController.PhaseGUI;
            Profiler.ResetStack();
        }

        public static void GUIPostfix() => ProfileController.phase = 0;
    }
}