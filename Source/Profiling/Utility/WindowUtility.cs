using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    public enum CurrentlyResizing { None, Window, Graph, SidePanel };

    public static class WindowUtility
    {
        public static CurrentlyResizing curStatus = CurrentlyResizing.None;
        public static bool resizingBottomLeft;
        public static bool resizingGraph;
        public static bool resizingSidePanel;
        public static float cachedSideWidth;

        public static Rect resizeStart = new Rect();
        private static Vector2 minWindowSize = new Vector2(150f, 150f);

        public static Rect ResizeAnalyzerWindow(Rect winRect)
        {
            Vector2 mousePosition = Event.current.mousePosition;

            /* |-------------------|
             * |   |              |+|
             * |   | V graph      |+| <-- Side Panel
             * |   |==============|+|
             * |   |               |
             * |------------------|+| <-- window    
             */


            // We want 3 'axis' of expansion here
            // - Bottom left - Expand the Window
            // - top of the graph draggable
            // - Middle left for dragging the side panel ta out

            Rect windowResizeRect = new Rect(winRect.width - 24f, winRect.height - 24f, 24f, 24f);
            Rect graphRect = new Rect(Panel_Tabs.width, winRect.height - (Window_Analyzer.GraphHeight + Window_Analyzer.DRAGGABLE_RECT_DIM + 18f), winRect.width - (Panel_Tabs.width + Window_Analyzer.DRAGGABLE_RECT_DIM + Window_Analyzer.SidePanelWidth), Window_Analyzer.DRAGGABLE_RECT_DIM);
            Rect sidePanelRect = new Rect(winRect.width - Window_Analyzer.DRAGGABLE_RECT_DIM, 24f, Window_Analyzer.DRAGGABLE_RECT_DIM, winRect.height - 24f);

            if (Event.current.type == EventType.MouseDown)
            {
                if (curStatus == CurrentlyResizing.None)
                {
                    if (Mouse.IsOver(windowResizeRect))
                    {
                        curStatus = CurrentlyResizing.Window;
                        resizeStart = new Rect(mousePosition.x, mousePosition.y, winRect.width, winRect.height);
                    }
                    else if (GUIController.CurrentCategory != Category.Settings) // We need to be in a window with entries to show the graph
                    {
                        if (Mouse.IsOver(graphRect))
                        {
                            curStatus = CurrentlyResizing.Graph;
                            resizeStart = new Rect(mousePosition.x, mousePosition.y, winRect.width, Window_Analyzer.GraphHeight);
                        }
                        else if (GUIController.CurrentProfiler != null && Settings.sidePanel) // We need an active profiler to show the side panel
                        {
                            if (Mouse.IsOver(sidePanelRect))
                            {
                                curStatus = CurrentlyResizing.SidePanel;
                                resizeStart = new Rect(mousePosition.x, mousePosition.y, winRect.width, winRect.height);
                                cachedSideWidth = winRect.width - Window_Analyzer.SidePanelWidth;
                            }
                        }
                    }

                    if (curStatus != CurrentlyResizing.None) // If our status has changed from None -> xxx, we want to consume the input to prevent drags starting etc
                    {
                        Event.current.Use();
                    }
                }
            }

            switch (curStatus)
            {
                case CurrentlyResizing.Window: HandleWindowResize(ref winRect); break;
                case CurrentlyResizing.Graph: HandleGraphResize(ref winRect); break;
                case CurrentlyResizing.SidePanel: HandleSideResize(ref winRect); break;
                default: break;
            }

#if DEBUG
            if (Settings.showGrapplingBoxes)
            {
                Widgets.DrawBoxSolid(windowResizeRect, Color.red);
                Widgets.DrawBoxSolid(graphRect, Color.red);
                Widgets.DrawBoxSolid(sidePanelRect, Color.red);
            }
#endif

            if(Event.current.type == EventType.MouseUp && curStatus != CurrentlyResizing.None)
            {
                curStatus = CurrentlyResizing.None;
                Event.current.Use();
            }

            Widgets.ButtonImage(windowResizeRect, TexUI.WinExpandWidget);
            Widgets.ButtonImage(new Rect(graphRect.x + (graphRect.width / 2) - 12f, graphRect.y, 24f, 24f), TexUI.WinExpandWidget);

            return new Rect(winRect.x, winRect.y, (int)winRect.width, (int)winRect.height);
        }

        public static void HandleWindowResize(ref Rect winRect)
        {
            var mousePosition = Event.current.mousePosition;

            winRect.width = resizeStart.width + (mousePosition.x - resizeStart.x);
            winRect.height = resizeStart.height + (mousePosition.y - resizeStart.y);
            if (winRect.width < minWindowSize.x)
            {
                winRect.width = minWindowSize.x;
            }
            if (winRect.height < minWindowSize.y)
            {
                winRect.height = minWindowSize.y;
            }
            winRect.xMax = Mathf.Min(UI.screenWidth, winRect.xMax);
            winRect.yMax = Mathf.Min(UI.screenHeight, winRect.yMax);
        }

        public static void HandleGraphResize(ref Rect winRect)
        {
            var mousePosition = Event.current.mousePosition;

            Window_Analyzer.GraphHeight = resizeStart.height + (resizeStart.y - mousePosition.y);
        }

        public static void HandleSideResize(ref Rect winRect)
        {
            var mousePosition = Event.current.mousePosition;

            winRect.width = resizeStart.width + (mousePosition.x - resizeStart.x);
            Window_Analyzer.SidePanelWidth = winRect.width - cachedSideWidth;

            Window_Analyzer.SidePanelWidth = Mathf.Clamp(Window_Analyzer.SidePanelWidth, 0, 400f); // This is an arbitrary number which looks good for me.

            if (winRect.width < Window_Analyzer.Initial.x) // Don't let people collapse the window further than the initial width
            {
                winRect.width = Window_Analyzer.Initial.x;
            }

            winRect.xMax = Mathf.Min(UI.screenWidth, winRect.xMax);
        }
    }
}

