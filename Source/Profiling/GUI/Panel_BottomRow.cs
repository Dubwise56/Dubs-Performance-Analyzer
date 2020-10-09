using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    public enum RowName
    {
        Graph,
        Stats,
        Patches,
        StackTrace
        /*, ChildProfilers */
    };

    class Panel_BottomRow
    {
        public static RowName currentRow = RowName.Graph;
        public static GeneralInformation? currentProfilerInformation;
        public static bool draggingWindow = false;
        public static bool windowSplit = false;
        public static bool draggingWindowSplit = false;
        public static bool currentlyDraggingRow = false;
        public static float windowSplitX = 0;
        public static RowName draggingRow = RowName.Graph;
        public static RowName[] splitActiveRows = {RowName.Graph, RowName.Stats};


        public static void Draw(Rect rect, Rect bigRect)
        {
            if (currentProfilerInformation == null || currentProfilerInformation.Value.method != GUIController.CurrentProfiler.meth)
            {
                GetGeneralSidePanelInformation();
            }

            HandleDrag(ref rect, bigRect);
            DrawButtonRow(ref rect);


            if (windowSplitX == 0)
            {
                windowSplitX = rect.center.x;
            }

            if (windowSplit)
            {
                var middleBar = new Rect(rect.x + windowSplitX, rect.y, Window_Analyzer.DRAGGABLE_RECT_DIM, rect.height);
                Widgets.DrawBoxSolid(middleBar, Widgets.WindowBGFillColor);
                Widgets.DrawHighlightIfMouseover(middleBar);

                rect.ContractedBy(2f);

                var leftSide = rect.LeftPartPixels(windowSplitX);
                Widgets.DrawMenuSection(leftSide);
                leftSide.ContractedBy(2f);

                var rightSide = rect.RetAdjustHorizonallyBy(windowSplitX + Window_Analyzer.DRAGGABLE_RECT_DIM);
                Widgets.DrawMenuSection(rightSide);
                rightSide.ContractedBy(2f);
                
                if (Input.GetMouseButtonDown(0) && Mouse.IsOver(middleBar) && draggingWindowSplit == false) draggingWindowSplit = true;

                if (draggingWindowSplit) windowSplitX = (Event.current.mousePosition.x - ( rect.x + Window_Analyzer.DRAGGABLE_RECT_DIM/2.0f));

                windowSplitX = Mathf.Clamp(windowSplitX, 80, rect.width - 80);

                if (Input.GetMouseButtonUp(0)) draggingWindowSplit = false;

                DrawActiveRow(splitActiveRows[0], leftSide);
                DrawActiveRow(splitActiveRows[1], rightSide);

                if (currentlyDraggingRow)
                {
                    Widgets.Label(new Rect(Event.current.mousePosition, new Vector2(draggingRow.ToString().GetWidthCached(), Text.LineHeight)), draggingRow.ToString());
                    var col = GUI.color;
                    GUI.color = Color.red;

                    if (Mouse.IsOver(rightSide.ExpandedBy(3f))) Widgets.DrawBox(rightSide.ExpandedBy(2f), 1);
                    if (Mouse.IsOver(leftSide.ExpandedBy(3f))) Widgets.DrawBox(leftSide.ExpandedBy(2f), 1);

                    GUI.color = col;
                }
            }
            else
            {
                Widgets.DrawMenuSection(rect);
                DrawActiveRow(currentRow, rect.ContractedBy(2f));
            }

            void DrawActiveRow(RowName row, Rect rect)
            {
                switch(row)
                {
                    case RowName.Graph: Panel_Graph.Draw(rect); break;
                    case RowName.Stats: Panel_Stats.DrawStats(rect); break;
                    case RowName.Patches: Panel_Patches.Draw(rect, currentProfilerInformation); break;
                    case RowName.StackTrace: Panel_StackTraces.Draw(rect, currentProfilerInformation); break;
                }
            }

        }

        public static void HandleDrag(ref Rect rect, Rect bigRect)
        {
            var graphDragRect = rect.TopPartPixels(Window_Analyzer.DRAGGABLE_RECT_DIM);
            rect.AdjustVerticallyBy(Window_Analyzer.DRAGGABLE_RECT_DIM);
            Widgets.DrawHighlightIfMouseover(graphDragRect);

            if (Input.GetMouseButtonDown(0) && Mouse.IsOver(graphDragRect) && draggingWindow == false)
                draggingWindow = true;

            if (draggingWindow) Window_Analyzer.GraphHeight = rect.height - ((Event.current.mousePosition.y - rect.y) + 5);

            Window_Analyzer.GraphHeight = Mathf.Clamp(Window_Analyzer.GraphHeight, 80, bigRect.height - 100f);

            if (Input.GetMouseButtonUp(0)) draggingWindow = false;
        }

        public static void DrawButtonRow(ref Rect rect)
        {
            var buttonRect = rect.LeftPartPixels(" StackTrace ".GetWidthCached());
            rect.AdjustHorizonallyBy(" StackTrace ".GetWidthCached());

            var anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;

            foreach (var r in Enum.GetValues(typeof(RowName)).Cast<int>())
            {
                var row = (RowName) r;

                var bRect = buttonRect.TopPartPixels(Text.LineHeight);
                buttonRect.AdjustVerticallyBy(Text.LineHeight);

                var rowString = row.ToString();
                Widgets.Label(bRect, rowString);

                if (Widgets.ButtonInvisible(bRect)) currentRow = row;
                Widgets.DrawHighlightIfMouseover(bRect);

                if (currentRow == row) Widgets.DrawHighlight(bRect);

                Widgets.DrawLineHorizontal(buttonRect.x, buttonRect.y, bRect.width);

                if (windowSplit)
                {
                    if (Input.GetMouseButtonDown(0) && Mouse.IsOver(bRect) && currentlyDraggingRow == false)
                    {
                        currentlyDraggingRow = true;
                        draggingRow = row;
                    }

                    if (Input.GetMouseButtonUp(0) && draggingRow == row && currentlyDraggingRow)
                    {
                        currentlyDraggingRow = false;

                        var leftSide = rect.LeftPartPixels(windowSplitX);
                        var rightSide = rect.RetAdjustHorizonallyBy(windowSplitX + Window_Analyzer.DRAGGABLE_RECT_DIM);
                        if (Mouse.IsOver(leftSide)) splitActiveRows[0] = row;
                        else if (Mouse.IsOver(rightSide)) splitActiveRows[1] = row;
                    }
                }
            }


            Text.Anchor = anchor;
            var checkboxRect = buttonRect.BottomPartPixels(Text.LineHeight);
            DubGUI.Checkbox(checkboxRect, "Split", ref windowSplit);
        }

        private static void GetGeneralSidePanelInformation()
        {
            var info = new GeneralInformation();

            info.method = GUIController.CurrentProfiler.meth;
            info.methodName = info.method.Name;
            info.typeName = info.method.DeclaringType.FullName;
            info.assname = info.method.DeclaringType.Assembly.FullName;
            info.modName = GetModName(info);
            info.patchType = "";
            info.patches = new List<GeneralInformation>();

            var patches = Harmony.GetPatchInfo(info.method);
            if (patches == null) // dunno if this ever could happen because surely we have patched it, but, whatever
            {
                currentProfilerInformation = info;
                return;
            }


            foreach (var patch in patches.Prefixes) CollectPatchInformation("Prefix", patch);
            foreach (var patch in patches.Postfixes) CollectPatchInformation("Postfix", patch);
            foreach (var patch in patches.Transpilers) CollectPatchInformation("Transpiler", patch);
            foreach (var patch in patches.Finalizers) CollectPatchInformation("Finalizer", patch);

            void CollectPatchInformation(string type, Patch patch)
            {
                if (patch.owner == Modbase.Harmony.Id) return;

                var subPatch = new GeneralInformation();
                subPatch.method = patch.PatchMethod;
                subPatch.typeName = patch.PatchMethod.DeclaringType.FullName;
                subPatch.methodName = patch.PatchMethod.Name;
                subPatch.assname = patch.PatchMethod.DeclaringType.Assembly.FullName;
                subPatch.modName = GetModName(subPatch);
                subPatch.patchType = type;

                info.patches.Add(subPatch);
            }

            currentProfilerInformation = info;
        }

        public static void OpenGithub(string fullMethodName)
        {
            Application.OpenURL(@"https://github.com/search?l=C%23&q=" + fullMethodName + "&type=Code");
        }

        public static void OpenDnspy(MethodBase method)
        {
            if (string.IsNullOrEmpty(Settings.PathToDnspy))
            {
                Log.ErrorOnce("[Analyzer] You have not given a local path to dnspy", 10293838);
                return;
            }

            var meth = method;
            var path = meth.DeclaringType.Assembly.Location;
            if (path == null || path.Length == 0)
            {
                var contentPack = LoadedModManager.RunningMods.FirstOrDefault(m =>
                    m.assemblies.loadedAssemblies.Contains(meth.DeclaringType.Assembly));
                if (contentPack != null)
                {
                    path = ModContentPack
                        .GetAllFilesForModPreserveOrder(contentPack, "Assemblies/", p => p.ToLower() == ".dll")
                        .Select(fileInfo => fileInfo.Item2.FullName)
                        .First(dll =>
                        {
                            var assembly = Assembly.ReflectionOnlyLoadFrom(dll);
                            return assembly.GetType(meth.DeclaringType.FullName) != null;
                        });
                }
            }

            var token = meth.MetadataToken;
            if (token != 0)
                Process.Start(Settings.PathToDnspy, $"\"{path}\" --select 0x{token:X8}");
        }

        private static string GetModName(GeneralInformation info)
        {
            if (info.assname.Contains("Assembly-CSharp")) return "Rimworld - Core";
            if (info.assname.Contains("UnityEngine")) return "Rimworld - Unity";
            if (info.assname.Contains("System")) return "Rimworld - System";
            try
            {
                return ModInfoCache.AssemblyToModname[info.assname];
            }
            catch (Exception)
            {
                return "Failed to locate assembly information";
            }
        }
    }
}