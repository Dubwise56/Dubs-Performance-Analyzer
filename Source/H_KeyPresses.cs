using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DubsAnalyzer
{
    [StaticConstructorOnStartup]
    internal static class H_KeyPresses
    {
        public static KeyBindingDef key = KeyBindingDef.Named("DubsOptimizerKey");
        public static KeyBindingDef restartkey = KeyBindingDef.Named("DubsOptimizerRestartKey");

        public static void PatchMe()
        {
            var biff = new HarmonyMethod(typeof(H_KeyPresses).GetMethod(nameof(pertwee)));
            var skiff = typeof(MapInterface).GetMethod(nameof(MapInterface.MapInterfaceOnGUI_BeforeMainTabs));
            Analyzer.perfharmony.Patch(skiff, biff);

            biff = new HarmonyMethod(typeof(H_KeyPresses).GetMethod(nameof(OnGUI)));
            skiff = typeof(UIRoot_Entry).GetMethod(nameof(UIRoot_Entry.UIRootOnGUI));
            Analyzer.perfharmony.Patch(skiff, biff);

            skiff = typeof(UIRoot_Play).GetMethod(nameof(UIRoot_Play.UIRootOnGUI));
            Analyzer.perfharmony.Patch(skiff, biff);
        }

        public static void OnGUI()
        {
           // if (Event.current.type == EventType.KeyDown)
           // {

                if (restartkey.KeyDownEvent)
                {
                    GenCommandLine.Restart();
                }

                try
                {
                    if (key != null && key.KeyDownEvent)
                    {
                        if (Find.WindowStack.WindowOfType<Dialog_Analyzer>() != null)
                        {
                            Find.WindowStack.RemoveWindowsOfType(typeof(Dialog_Analyzer));
                        }
                        else
                        {
                            Find.WindowStack.Add(new Dialog_Analyzer());
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }
            // }
        }

        public static void pertwee()
        {

        }
    }
}