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
using RimWorld;
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

    public class BottomRowPanel
    {
        public BottomRowPanel(RowName row, float xStart, float width)
        {
            type = row;
            this.width = width;
            this.xStart = xStart;
            dragging = false;
        }

        public RowName type;
        public float xStart;
        public float width;
        public bool dragging;
    }

    /*
     * ___________________________
     * | Stats |X| | Graph     |X|
     * |---------| |-------------|
     * |         | |             |
     * |         | |             |
     * |         | |             |
     * |         | |             |
     * |_________|_|_____________|
     */

    class Panel_BottomRow
    {
        public static GeneralInformation? currentProfilerInformation;
        public static List<BottomRowPanel> panels = new List<BottomRowPanel>{ new BottomRowPanel(RowName.Graph, 0, 350) };


        public static void Draw(Rect rect, Rect bigRect)
        {
            if (currentProfilerInformation == null || (GUIController.CurrentProfiler != null && currentProfilerInformation.Value.method != GUIController.CurrentProfiler.meth))
            {
                GetGeneralSidePanelInformation();
            }

            rect.width -= Window_Analyzer.DRAGGABLE_RECT_DIM;
            rect.AdjustVerticallyBy(10f);

            panels[panels.Count - 1].width = 
                (panels.Count <= 1) ? 
                    rect.width : 
                    rect.width - (panels[panels.Count - 2].xStart + panels[panels.Count - 2].width) - 18;

            var buttonColumn = rect.LeftPartPixels(" + ".GetWidthCached());
            rect.AdjustHorizonallyBy(" + ".GetWidthCached());

            DrawButtonColumn(buttonColumn, rect.width);

            for(int i = panels.Count - 1; i >= 0; i--)
            {
                var panel = panels[i];

                var panelRect = new Rect(rect.x + panel.xStart, rect.y, panel.width, rect.height);

                if (i != 0) // Drag Rct
                {
                    var r = new Rect(rect.x + (panel.xStart - Window_Analyzer.DRAGGABLE_RECT_DIM), rect.y, Window_Analyzer.DRAGGABLE_RECT_DIM, rect.height);
                   
                    Widgets.DrawHighlightIfMouseover(r);

                    if (Input.GetMouseButtonDown(0) && Mouse.IsOver(r) && !panel.dragging) panel.dragging = true;

                    if (panel.dragging)
                    {
                        var newPos = Event.current.mousePosition.x - (rect.x - Window_Analyzer.DRAGGABLE_RECT_DIM / 2.0f);
                        var delta = panel.xStart - newPos;
                        panel.xStart = newPos;
                        panel.width += delta;
                        panels[i - 1].width -= delta;
                    }

                    if (Input.GetMouseButtonUp(0)) panel.dragging = false;
                }

                Widgets.DrawMenuSection(panelRect);
                {
                    var topRect = panelRect.TopPartPixels(Text.LineHeight);
                    panelRect.AdjustVerticallyBy(Text.LineHeight);
                    Widgets.DrawLineHorizontal(panelRect.x, panelRect.y, panelRect.width);

                    panelRect = panelRect.ContractedBy(2f);

                    var label = "  " + panel.type.ToString();

                    var leftPartRect = topRect.LeftPartPixels(label.GetWidthCached());
                    Widgets.Label(leftPartRect, label);

                    if (Widgets.ButtonImage(topRect.RightPartPixels(Text.LineHeight), ResourceCache.GUI.Menu))
                    {
                        var enums = typeof(RowName).GetEnumValues();
                        var list = (from object e in enums select new FloatMenuOption(((RowName) e).ToString(), () => panel.type = (RowName) e)).ToList();
                        Find.WindowStack.Add(new FloatMenu(list));
                    }


                }
                
                panelRect.AdjustVerticallyBy(2f);

                switch(panel.type)
                {
                    case RowName.Graph: Panel_Graph.Draw(panelRect); break;
                    case RowName.Stats: Panel_Stats.DrawStats(panelRect); break;
                    case RowName.Patches: Panel_Patches.Draw(panelRect, currentProfilerInformation); break;
                    case RowName.StackTrace: Panel_StackTraces.Draw(panelRect, currentProfilerInformation); break;
                }
            }

        }

        private static void DrawButtonColumn(Rect rect, float availWidth)
        {
            if (Widgets.ButtonText(rect.TopPartPixels(Text.LineHeight), " + "))
            {
                var widthMinusGrabBars = availWidth - (18 * panels.Count);
                var avWidth = widthMinusGrabBars / (panels.Count + 1.0f);

                var increment = avWidth / panels.Count;
                increment = Mathf.Round(increment);

                for(int i = 0; i < panels.Count; i++)
                {
                    var panel = panels[i];
                    panel.width -= increment;
                    panel.xStart += increment * (panels.Count - i);
                }

                if (panels.Count >= 1)
                {
                    panels[0].xStart += 18;
                    panels[0].width -= 18;
                    panels.Insert(0, new BottomRowPanel(RowName.Graph, 0, panels[0].xStart - 18));
                }
                else
                {
                    panels.Add(new BottomRowPanel(RowName.Graph, 0, availWidth));
                }
            }
            rect.AdjustVerticallyBy(Text.LineHeight);

            if (Widgets.ButtonText(rect.TopPartPixels(Text.LineHeight), " - "))
            {
                if (panels.Count == 0) return;

                var delta = panels[0].width + 18;
                var increment = delta / ( panels.Count - 1.0f );

                for(int i = 0; i < panels.Count; i++)
                {
                    var panel = panels[i];
                    panel.width += increment;
                    panel.xStart -= (increment * (panels.Count - i));
                }

                if (panels.Count == 2)
                {
                    panels[1].xStart = 0;
                    panels[1].width = availWidth;
                } else if (panels.Count > 2)
                {
                    panels[1].xStart = 0;
                    panels[1].width = panels[2].xStart - 18;
                }

                panels.RemoveAt(0);
            }
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