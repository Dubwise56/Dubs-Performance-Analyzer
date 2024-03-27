using System;
using Analyzer.Performance;
using Analyzer.Profiling;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Analyzer
{
    [StaticConstructorOnStartup]
    internal static class H_KeyPresses
    {
        private static KeyBindingDef key;
        private static KeyBindingDef restartkey;
        private static KeyBindingDef alertKey;
        private static bool attemptedInitialise = false;

        static H_KeyPresses()
        {
            var _ = AttemptToInitialiseKeys();
        }
        
        public static bool AttemptToInitialiseKeys()
        {
            attemptedInitialise = true;
            try
            {
                key = KeyBindingDef.Named("DubsOptimizerKey");
                restartkey = KeyBindingDef.Named("DubsOptimizerRestartKey");
                alertKey = KeyBindingDef.Named("dpa_ToggleAlertBlock");
                
                var biff = new HarmonyMethod(typeof(H_KeyPresses).GetMethod(nameof(OnGUI)));
                var skiff = typeof(UIRoot_Entry).GetMethod(nameof(UIRoot_Entry.UIRootOnGUI));
                Modbase.StaticHarmony.Patch(skiff, biff);

                skiff = typeof(UIRoot_Play).GetMethod(nameof(UIRoot_Play.UIRootOnGUI));
                Modbase.StaticHarmony.Patch(skiff, biff);
            }
            catch (Exception e)
            {
                ThreadSafeLogger.ReportException(e, "Failed to load keybindings");
                return false;
            }

            return true;
        }

        public static void OnGUI()
        {
            try
            {
                if (restartkey.KeyDownEvent)
                {
                    GenCommandLine.Restart();
                }
                
                if (key != null && key.KeyDownEvent)
                {
                    if (Find.WindowStack.WindowOfType<Window_Analyzer>() != null)
                    {
                        Find.WindowStack.RemoveWindowsOfType(typeof(Window_Analyzer));
                    }
                    else
                    {
                        Find.WindowStack.Add(new Window_Analyzer());
                    }
                }
                
                if (alertKey.KeyDownEvent)
                {
                    H_AlertsReadoutUpdate.DisableAlerts = !H_AlertsReadoutUpdate.DisableAlerts;
                }
            }
            catch (Exception e)
            {
                if ( (attemptedInitialise is false) && AttemptToInitialiseKeys()) return;
                
                if(Settings.verboseLogging)
                    ThreadSafeLogger.ReportException(e, "Error while handling analyzer keybindings");
            }
        }
    }
}
