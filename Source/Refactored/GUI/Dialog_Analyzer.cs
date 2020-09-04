using ColourPicker;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;
using Verse;

/*  Naming Wise
 *  Tabs on the side, Ex 'HarmonyPatches', SideTab
 *  Categories for them, Ex 'Tick', SideTabCategories
 *  A Log 'inside' a SideTab, is a 'Log', each Log belongs to a SideTab
 */

namespace Analyzer
{

    [StaticConstructorOnStartup]
    public class Dialog_Analyzer : Window
    {
        public static List<Action> QueuedMessages = new List<Action>();
        public static object messageSync = new object();

        public override void PreOpen()
        {
            base.PreOpen();
            Analyzer.BeginProfiling();
        }

        public static void LoadModes()
        {
            List<Type> entries = GenTypes.AllTypes.Where(m => m.TryGetAttribute<Entry>(out _)).OrderBy(m => m.TryGetAttribute<Entry>().name).ToList();

            foreach (Type entryType in entries)
            {
                try
                {
                    Entry entry = entryType.TryGetAttribute<Entry>();
                    entry.Settings = new Dictionary<FieldInfo, Setting>();

                    foreach (FieldInfo fieldInfo in entryType.GetFields().Where(m => m.TryGetAttribute<Setting>(out _)))
                    {
                        Setting sett = fieldInfo.TryGetAttribute<Setting>();
                        entry.Settings.SetOrAdd(fieldInfo, sett);
                    }

                    entry.onMouseOver = AccessTools.Method(entryType, "MouseOver");
                    entry.onClick = AccessTools.Method(entryType, "Clicked");
                    entry.onSelect = AccessTools.Method(entryType, "Selected");
                    entry.type = entryType;

                    // Find and append Entry to the correct Tab
                    // TODO
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }
            }

            // Loop through our static instances and add them to the Correct Tab
            // foreach (Entry entry in Entry.entries)
        }

        public override void PostClose()
        {
            base.PostClose();
            Analyzer.EndProfiling();
            Modbase.Settings.Write();

            // Pend the cleaning up of all of our state.
            Analyzer.Cleanup();
        }

        public Dialog_Analyzer()
        {
            layer = WindowLayer.Super;
            forcePause = false;
            absorbInputAroundWindow = false;
            closeOnCancel = false;
            soundAppear = SoundDefOf.CommsWindow_Open;
            soundClose = SoundDefOf.CommsWindow_Close;
            doCloseButton = false;
            doCloseX = true;
            draggable = true;
            drawShadow = true;
            preventCameraMotion = false;
            onlyOneOfTypeAllowed = true;
            resizeable = true;
        }


        public override void DoWindowContents(Rect inRect)
        {
            
        }
    }
}