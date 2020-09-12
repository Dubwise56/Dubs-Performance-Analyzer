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
        public const float TOP_ROW_HEIGHT = 20f;
        public const float DRAGGABLE_RECT_DIM = 18f;

        public static Vector2 Initial = new Vector2(890, 650);
        public override Vector2 InitialSize => Initial;
        public override float Margin => 0;
        public static bool firstOpen = true;
        public static float GraphHeight = 220f;
        public static float SidePanelWidth = 0f;


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
            GUIController.CurrentProfiler = null;
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
            resizeable = false;
            closeOnCancel = false;
            closeOnAccept = false;
            draggable = false;
        }

        public void HandleWindowMovement()
        {
            if (Event.current.type != EventType.Repaint)
            {
                Rect lhs = WindowUtility.ResizeAnalyzerWindow(windowRect);
                if (lhs != windowRect)
                {
                    resizeLater = true;
                    resizeLaterRect = lhs;
                }
            }

            if (Event.current.type == EventType.Repaint)
            {
                WindowUtility.ResizeAnalyzerWindow(windowRect);
            }
        }

        public override void WindowOnGUI()
        {
            if (resizeLater)
            {
                windowRect = resizeLaterRect;
                resizeLater = false;
            }

            base.WindowOnGUI();
        }


        public override void DoWindowContents(Rect rect)
        {
            // Do Window Resizing
            HandleWindowMovement();

            Action handleDrag = () => GUI.DragWindow(rect);

            rect = rect.ContractedBy(18f); // Adjust by our (removed) margin

            // Display our logged messages that we may have recieved from other threads.
            ThreadSafeLogger.DisplayLogs();

            Panel_Tabs.Draw(rect, GUIController.Tabs);

            rect.AdjustHorizonallyBy(Panel_Tabs.width); // Shift the x and shrink the width by the width of the Tabs

            if (GUIController.GetCurrentCategory == Category.Settings)
            {
                Panel_Settings.Draw(rect);
                handleDrag();

                return;
            }

            // We are in one of our entry-filled tabs. 
            /*  - Conditionally Change our Rect size (for side panel)
             *  - Draw our Top Row (always)
             *  - Draw our active logs (always)
             *  - Draw our graph (if there is a current profiler)
             *  - Draw our side panel (if there is a current profile && the setting is enabled)
             */

            if (Settings.sidePanel && GUIController.CurrentProfiler != null)
            {
                rect.width -= SidePanelWidth;
            }

            Panel_TopRow.Draw(rect.TopPartPixels(TOP_ROW_HEIGHT));

            rect.AdjustVerticallyBy(TOP_ROW_HEIGHT);

            if (GUIController.CurrentProfiler == null)
            {
                Panel_Logs.DrawLogs(rect);
                handleDrag();

                return;
            }

            // If there is a current profiler, we need to adjust the height of the logs 
            rect.height -= (GraphHeight + DRAGGABLE_RECT_DIM);
            Panel_Logs.DrawLogs(rect);

            // Move our rect down to just below the Logs 
            rect.y += GraphHeight;
            rect.height += DRAGGABLE_RECT_DIM;
            rect = rect.BottomPartPixels(GraphHeight + DRAGGABLE_RECT_DIM);

            Widgets.DrawHighlightIfMouseover(rect.TopPartPixels(DRAGGABLE_RECT_DIM));

            rect.AdjustVerticallyBy(DRAGGABLE_RECT_DIM);
            Panel_Graph.Draw(rect);

            if (Settings.sidePanel && SidePanelWidth >= 100)
            {
                var sidePanelRect = new Rect(rect.xMax, 20f, SidePanelWidth, windowRect.height - 20f);
                Panel_SideInfo.Draw(sidePanelRect);
            }

            handleDrag();
        }
    }
}