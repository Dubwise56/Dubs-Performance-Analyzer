using HarmonyLib;
using System;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    [Entry("entry.update.frametimes", Category.Update)]
    public class H_RootUpdate
    {
        public static bool Active = false;

        public static void Prefix()
        {
            if (Active)
                ProfileController.Start("Game Update");

            if (GUIController.CurrentCategory != Category.Tick)
                ProfileController.BeginUpdate();
        }

        public static void Postfix()
        {
            if (Active)
            {
                ProfileController.Stop("Frame times");
                ProfileController.Stop("Game Update");
            }

            if (GUIController.CurrentCategory != Category.Tick) // If we are tick, we will 'update' in the TickManager.DoSingleTick method
                ProfileController.EndUpdate();

            if (Active)
                ProfileController.Start("Frame times");

        }
    }
}