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
    public enum ProfileInfoMode
    {
        Graph,
        Stats,
        Patches,
        StackTrace,
        Save
        /*, ChildProfilers */
    };

    public class BottomRowPanel
    {
        public BottomRowPanel(ProfileInfoMode row, float xStart, float width)
        {
            type = row;
            this.width = width;
            this.xStart = xStart;
            dragging = false;
            if (row == ProfileInfoMode.Graph) graph = new Panel_Graph();
        }

        public ProfileInfoMode type;
        public float xStart;
        public float width;
        public bool dragging;
        public Panel_Graph graph;
    }

    public class Panel_BottomRow
    {
        public static GeneralInformation? currentProfilerInformation;

        public static ProfileInfoMode ProfileInfoTab;

        private static Panel_Graph graph = new Panel_Graph();
        private static Panel_Patches patches = new Panel_Patches();
        private static Panel_Save save = new Panel_Save();
        private static Panel_StackTraces stacktraces = new Panel_StackTraces();

        static Rect tabRect = new Rect(0, 0, 150, 18);
        public static void DrawTab(Rect r, ProfileInfoMode i, string lab)
        {
            r.height += 1;
            r.width += 1;
            Widgets.DrawMenuSection(r);
            if (ProfileInfoTab == i)
            {
                var hang = r.ContractedBy(1f);
                hang.y += 2;
                Widgets.DrawBoxSolid(hang, Widgets.MenuSectionBGFillColor);
            }

            Widgets.Label(r, lab);

            if (Widgets.ButtonInvisible(r))
            {
                ProfileInfoTab = i;
                Modbase.Settings.Write();
            }
        }

        public static void NotifyNewProfiler(Profiler prev, Profiler next) {
            ResetCurrentPanel();
            GetGeneralSidePanelInformation();
            
            // are we in the patches category, but the method has no patches?
            if (ProfileInfoTab == ProfileInfoMode.Patches && (currentProfilerInformation?.patches.Any() ?? false))
                ProfileInfoTab = ProfileInfoMode.Graph;
                
            // are we in the stack trace category, but the profiler has no method?
            if (ProfileInfoTab == ProfileInfoMode.StackTrace && currentProfilerInformation?.method != null)
                ProfileInfoTab = ProfileInfoMode.Graph;
        }

        public static void Draw(Rect rect)
        {
            if (GUIController.CurrentProfiler == null) return;

            var statbox = rect;
            statbox.width = Panel_Tabs.width - 10;
            
            var pRect = rect;
            pRect.x = statbox.xMax + 10;
            pRect.width -= statbox.xMax;
            pRect.AdjustVerticallyBy(tabRect.height);

            Widgets.DrawMenuSection(statbox);

            Panel_Stats.DrawStats(statbox, currentProfilerInformation);

            Widgets.DrawMenuSection(pRect);

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;

            tabRect.width = Mathf.Min(150f, pRect.width / 3f);

            tabRect.x = pRect.x;
            tabRect.y = pRect.y - tabRect.height;
            DrawTab(tabRect, ProfileInfoMode.Graph, "Graph");
            tabRect.x = tabRect.xMax;
            if (currentProfilerInformation?.patches.Any() ?? false)
            {
                DrawTab(tabRect, ProfileInfoMode.Patches, "Patches");
                tabRect.x = tabRect.xMax;
            }   
            if (currentProfilerInformation?.method != null)
            {
                DrawTab(tabRect, ProfileInfoMode.StackTrace, "Stacktrace");
                tabRect.x = tabRect.xMax;
            }
            DrawTab(tabRect, ProfileInfoMode.Save, "Save and Compare");
            tabRect.x = tabRect.xMax;

            DubGUI.ResetFont();

            switch (ProfileInfoTab)
            {
                case ProfileInfoMode.Graph: graph.Draw(pRect.ContractedBy(1)); break;
                case ProfileInfoMode.Patches: patches.Draw(pRect, currentProfilerInformation); break;
                case ProfileInfoMode.StackTrace: stacktraces.Draw(pRect, currentProfilerInformation); break;
                case ProfileInfoMode.Save: save.Draw(pRect, currentProfilerInformation); break;
            }
        }

        private static void ResetCurrentPanel()
        {
            switch (ProfileInfoTab)
            {
                case ProfileInfoMode.Patches: patches.ResetState(currentProfilerInformation); break;
                case ProfileInfoMode.StackTrace: stacktraces.ResetState(currentProfilerInformation); break;
                case ProfileInfoMode.Save: save.ResetState(currentProfilerInformation); break;
            } 
        }

        private static void GetGeneralSidePanelInformation()
        {
            currentProfilerInformation = null;
            if (GUIController.CurrentProfiler?.meth == null) return;

            var info = new GeneralInformation();

            info.method = GUIController.CurrentProfiler.meth;
            info.methodName = info.method.Name;
            info.typeName = info.method.DeclaringType.FullName;
            info.assname = info.method.DeclaringType.Assembly.FullName.Split(',').First();
            info.modName = GetModName(info.method.DeclaringType.Assembly.FullName);
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
                if (!Utility.IsNotAnalyzerPatch(patch.owner)) return;

                var subPatch = new GeneralInformation
                {
                    method = patch.PatchMethod,
                    typeName = patch.PatchMethod.DeclaringType.FullName,
                    methodName = patch.PatchMethod.Name,
                    assname = patch.PatchMethod.DeclaringType.Assembly.FullName.Split(',').First(),
                    modName = GetModName(patch.PatchMethod.DeclaringType.Assembly.FullName),
                    patchType = type
                };

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

        private static string GetModName(string info)
        {
            if (info.Contains("Assembly-CSharp")) return "Rimworld - Core";
            if (info.Contains("UnityEngine")) return "Rimworld - Unity";
            if (info.Contains("System")) return "Rimworld - System";

            if (ModInfoCache.AssemblyToModname.TryGetValue(info, out var value)) return value;

            return "Failed to locate assembly information";
        }
    }
}