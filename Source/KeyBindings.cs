using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Analyzer
{

    [StaticConstructorOnStartup]
    internal static class H_KeyPresses
    {
        public static KeyBindingDef key;
        public static KeyBindingDef restartkey;

        static H_KeyPresses()
        {
            key = KeyBindingDef.Named("DubsOptimizerKey");
            restartkey = KeyBindingDef.Named("DubsOptimizerRestartKey");
            PatchMe(Modbase.StaticHarmony);
        }

        public static void PatchMe(Harmony harmony)
        {
            var biff = new HarmonyMethod(typeof(H_KeyPresses).GetMethod(nameof(pertwee)));
            var skiff = typeof(MapInterface).GetMethod(nameof(MapInterface.MapInterfaceOnGUI_BeforeMainTabs));
            harmony.Patch(skiff, biff);

            biff = new HarmonyMethod(typeof(H_KeyPresses).GetMethod(nameof(OnGUI)));
            skiff = typeof(UIRoot_Entry).GetMethod(nameof(UIRoot_Entry.UIRootOnGUI));
            harmony.Patch(skiff, biff);

            skiff = typeof(UIRoot_Play).GetMethod(nameof(UIRoot_Play.UIRootOnGUI));
            harmony.Patch(skiff, biff);
        }

        public static void OnGUI()
        {
            if (restartkey.KeyDownEvent)
            {
                GenCommandLine.Restart();
            }

            try
            {
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
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }

        public static void pertwee()
        {

        }
    }
}
