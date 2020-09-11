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

namespace Analyzer
{

    public class Window_Analyzer : Window
    {
        public override Vector2 InitialSize => new Vector2(890, 650);
        public static bool firstOpen = true;

        public override void SetInitialSizeAndPosition()
        {
            windowRect = new Rect(50f, (UI.screenHeight - InitialSize.y) / 2f, InitialSize.x, InitialSize.y);
            windowRect = windowRect.Rounded();
        }

        public override void PreOpen()
        {
            base.PreOpen();

            if (firstOpen) // If we have not been opened yet, load all our entries
            {
                LoadEntries();
                firstOpen = false;
            }

            Analyzer.BeginProfiling();

            if (!Modbase.isPatched)
                Modbase.Harmony.PatchAll();
        }

        public override void PostClose()
        {
            base.PostClose();
            Analyzer.EndProfiling();
            GUIController.CurrentEntry?.SetActive(false);

            Modbase.Settings.Write();

            // Pend the cleaning up of all of our state.
            Analyzer.Cleanup();
        }

        public static void LoadEntries()
        {
            List<Type> allEntries = GenTypes.AllTypes.Where(m => m.TryGetAttribute<Entry>(out _)).OrderBy(m => m.TryGetAttribute<Entry>().name).ToList();

            foreach (Type entryType in allEntries)
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
                    if (!GUIController.Tab(entry.category).entries.ContainsKey(entry))
                        GUIController.Tab(entry.category).entries.Add(entry, entryType);
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }
            }

            // Loop through our static instances and add them to the Correct Tab
            foreach (Entry entry in Entry.entries)
            {
                if (!GUIController.Tab(entry.category).entries.ContainsKey(entry))
                    GUIController.Tab(entry.category).entries.Add(entry, entry.type);
            }

        }

        public Window_Analyzer()
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
            closeOnCancel = false;
            closeOnAccept = false;
        }


        public override void DoWindowContents(Rect inRect)
        {
            // Display our logged messages :)
            ThreadSafeLogger.DisplayLogs();

            var tabs = GUIController.Tabs;

            Panel_Tabs.Draw(inRect, tabs);

            inRect.AdjustHorizonallyBy(Panel_Tabs.width);

            if (GUIController.GetCurrentCategory == Category.Settings)
            {
                Panel_Settings.Draw(inRect);
                return;
            }

            Panel_TopRow.Draw(inRect.TopPartPixels(20f));

            inRect.AdjustVerticallyBy(20f);

            if (GUIController.CurrentProfiler != null)
            {
                inRect.height -= 230;
                Panel_Logs.DrawLogs(inRect);

                inRect.y += 220;
                inRect.height += 10;
                inRect = inRect.BottomPartPixels(220);
                Panel_Graph.Draw(inRect);
            }
            else
            {
                Panel_Logs.DrawLogs(inRect);
            }

        }
    }
}