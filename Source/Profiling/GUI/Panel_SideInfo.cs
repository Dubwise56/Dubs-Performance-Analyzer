using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    public struct GeneralInformation
    {
        public MethodBase method;

        public string modName;
        public string assname;
        public string methodName;
        public string typeName;

        public string patchType;
        public List<GeneralInformation> patches;
    }

    public static class Panel_SideInfo
    {
        private static Listing_Standard listing = null;
        private static Vector2 ScrollPosition = Vector2.zero;
        private static GameFont font = GameFont.Tiny;

        private static GeneralInformation? currentInformation = null;

        private static bool hideStats;


        public static void Draw(Rect rect)
        {
            listing = new Listing_Standard();

            Rect ListerBox = rect;
            ListerBox.width -= 10f;
            Widgets.DrawMenuSection(ListerBox);
            ListerBox = ListerBox.AtZero();

            { // Begin Scope for Scroll & GUI Group/View
                Widgets.BeginScrollView(rect, ref ScrollPosition, ListerBox);
                GUI.BeginGroup(ListerBox);
                listing.Begin(ListerBox);

                DrawGeneralSidePanel();
                DrawStatisticsSidePanel();

                listing.End();
                GUI.EndGroup();
                Widgets.EndScrollView();
            }
        }

        private static void DrawGeneralSidePanel()
        {
            DubGUI.Heading(listing, "General");
            Text.Font = font;

            if (GUIController.CurrentProfiler?.meth != null)
            {
                var currentMeth = GUIController.CurrentProfiler.meth;
                if (currentInformation == null || currentInformation.Value.method != currentMeth)
                {
                    GetGeneralSidePanelInformation();
                }
                var info = currentInformation.Value;

                DubGUI.InlineDoubleMessage($" Mod: {info.modName}", $" Assembly: {info.assname.Split(',').First()}.dll", listing, true);

                TextAnchor anch = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleCenter;
                string str = $"{info.typeName}:{info.methodName}";
                float strLen = Text.CalcHeight(str, listing.ColumnWidth * .95f);

                Rect rect = listing.GetRect(strLen);
                Widgets.Label(rect, str);

                Widgets.DrawHighlightIfMouseover(rect);
                if (Input.GetMouseButtonDown(1) && rect.Contains(Event.current.mousePosition)) // mouse button right
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>()
                    {
                        new FloatMenuOption("Open In Github", () => OpenGithub($"{info.typeName}.{info.methodName}")),
                        new FloatMenuOption("Open In Dnspy (requires local path)", () => OpenDnspy(info.method))
                    };

                    Find.WindowStack.Add(new FloatMenu(options));
                }

                if (info.patches.Count != 0) // This method is patched by someone other than us :-)
                {
                    listing.GapLine(0f);
                    DubGUI.Heading(listing, "Patches");

                    foreach (var patch in info.patches)
                    {
                        var patchRect = DubGUI.InlineDoubleMessage($"{patch.patchType}", $"{patch.modName}", listing, true);

                        Widgets.DrawHighlightIfMouseover(patchRect);

                        if (Mouse.IsOver(patchRect))
                        {
                            // todo cache tip
                            TooltipHandler.TipRegion(patchRect, $"Mod Name: {patch.modName}\nPatch Type: {patch.patchType}\nPatch Method: {patch.typeName}:{patch.methodName}");
                        }

                        if (Input.GetMouseButtonDown(1) && patchRect.Contains(Event.current.mousePosition)) // mouse button right
                        {
                            List<FloatMenuOption> options = new List<FloatMenuOption>()
                            {
                                new FloatMenuOption("Open In Github", () => OpenGithub($"{patch.typeName}.{patch.methodName}")),
                                new FloatMenuOption("Open In Dnspy (requires local path)", () => OpenDnspy(patch.method))
                            };

                            Find.WindowStack.Add(new FloatMenu(options));
                        }
                    }
                }

            }
            else
            {
                listing.Label("Failed to grab the method associated with this entry - please report this");
            }
            listing.GapLine(0f);
        }

        private static void GetGeneralSidePanelInformation()
        {
            GeneralInformation info = new GeneralInformation();

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
                currentInformation = info;
                return;
            }


            foreach (Patch patch in patches.Prefixes) CollectPatchInformation("Prefix", patch);
            foreach (Patch patch in patches.Postfixes) CollectPatchInformation("Postfix", patch);
            foreach (Patch patch in patches.Transpilers) CollectPatchInformation("Transpiler", patch);
            foreach (Patch patch in patches.Finalizers) CollectPatchInformation("Finalizer", patch);

            void CollectPatchInformation(string type, Patch patch)
            {
                if (patch.owner == Modbase.Harmony.Id) return;

                GeneralInformation subPatch = new GeneralInformation();
                subPatch.method = patch.PatchMethod;
                subPatch.typeName = patch.PatchMethod.DeclaringType.FullName;
                subPatch.methodName = patch.PatchMethod.Name;
                subPatch.assname = patch.PatchMethod.DeclaringType.Assembly.FullName;
                subPatch.modName = GetModName(subPatch);
                subPatch.patchType = type;

                info.patches.Add(subPatch);
            }

            currentInformation = info;
        }

        private static void OpenGithub(string fullMethodName)
        {
            Application.OpenURL(@"https://github.com/search?l=C%23&q=" + fullMethodName + "&type=Code");
        }

        private static void OpenDnspy(MethodBase method)
        {
            if (Settings.PathToDnspy == "" || Settings.PathToDnspy == null)
            {
                Log.ErrorOnce("You have not given a local path to dnspy", 10293838);
                return;
            }

            MethodBase meth = method;
            string path = meth.DeclaringType.Assembly.Location;
            if (path == null || path.Length == 0)
            {
                ModContentPack contentPack = LoadedModManager.RunningMods.FirstOrDefault(m => m.assemblies.loadedAssemblies.Contains(meth.DeclaringType.Assembly));
                if (contentPack != null)
                {
                    path = ModContentPack.GetAllFilesForModPreserveOrder(contentPack, "Assemblies/", p => p.ToLower() == ".dll", null)
                        .Select(fileInfo => fileInfo.Item2.FullName)
                        .First(dll =>
                        {
                            Assembly assembly = Assembly.ReflectionOnlyLoadFrom(dll);
                            return assembly.GetType(meth.DeclaringType.FullName) != null;
                        });
                }
            }
            int token = meth.MetadataToken;
            if (token != 0)
                Process.Start(Settings.PathToDnspy, $"\"{path}\" --select 0x{token:X8}");

        }

        private static string GetModName(GeneralInformation info)
        {
            if (info.assname.Contains("Assembly-CSharp")) return "Rimworld - Core";
            else if (info.assname.Contains("UnityEngine")) return "Rimworld - Unity";
            else if (info.assname.Contains("System")) return "Rimworld - System";
            else
            {
                try
                {
                    return ModInfoCache.AssemblyToModname[info.assname];
                }
                catch (Exception) { return "Failed to locate assembly information"; }
            }
        }

        private static void DrawStatisticsSidePanel()
        {
            DubGUI.CollapsableHeading(listing, "Statistics", ref hideStats);
            if (hideStats) return;
            Text.Font = font;

            LogStats s = new LogStats();
            s.GenerateStats();

            if (CurrentLogStats.stats == null)
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                listing.Label($"Loading{GenText.MarchingEllipsis(0f)}");
                DubGUI.ResetFont();
            }
            else
            {
                lock (CurrentLogStats.sync)
                {
                    DubGUI.InlineTripleMessage($" Entries: {CurrentLogStats.stats.Entries}", $" Σ Calls: {CurrentLogStats.stats.TotalCalls}", $" Σ Time: {CurrentLogStats.stats.TotalTime:0.000}ms ", listing, true);
                    DubGUI.InlineTripleMessage($" μ time (per call): {CurrentLogStats.stats.MeanTimePerCall:0.000}ms ", $" μ calls (per update): {CurrentLogStats.stats.MeanCallsPerUpdateCycle:0.00}", $" μ time (per update): {CurrentLogStats.stats.MeanTimePerUpdateCycle:0.000}ms ", listing, true);
                    DubGUI.InlineDoubleMessage($" Median calls {CurrentLogStats.stats.MedianCalls} ", $" Median time {CurrentLogStats.stats.MedianTime} ", listing, true);
                    DubGUI.InlineDoubleMessage($" Highest Time: {CurrentLogStats.stats.HighestTime:0.000}ms", $" Highest Calls (per frame): {CurrentLogStats.stats.HighestCalls}", listing, true);
                }
            }
        }

        private static void DrawStackTraceSidePanel()
        {
            //if (Modbase.Settings.AdvancedMode)
            //{
            //    DubGUI.CollapsableHeading(listing, "Stack Trace", ref AnalyzerState.HideStacktrace);
            //    Text.Font = font;
            //}

            //if (!AnalyzerState.HideStacktrace)
            //{
            //    listing.Label($"Stacktraces: {StackTraceRegex.traces.Count}");

            //    foreach (KeyValuePair<string, StackTraceInformation> st in StackTraceRegex.traces.OrderBy(w => w.Value.Count).Reverse())
            //    {
            //        int i = 0;
            //        StackTraceInformation traceInfo = st.Value;

            //        for (i = 0; i < st.Value.TranslatedArr().Count() - 2; i++)
            //        {
            //            DrawTrace(i, false);
            //        }

            //        DrawTrace(i, true);

            //        void DrawTrace(int idx, bool capoff)
            //        {
            //            Rect rect = DubGUI.InlineDoubleMessage(
            //                traceInfo.TranslatedArr()[idx], traceInfo.methods[idx].Item2.Count.ToString(), listing,
            //                capoff).LeftPart(.5f);

            //            if (Mouse.IsOver(rect))
            //            {
            //                StringBuilder builder = new StringBuilder();
            //                foreach (StackTraceInformation.HarmonyPatch p in traceInfo.methods[idx].Item2)
            //                    GetString(p);

            //                void GetString(StackTraceInformation.HarmonyPatch patch)
            //                {
            //                    if (patch.id != Modbase.Harmony.Id && patch.id != InternalMethodUtility.Harmony.Id)
            //                    {
            //                        string ass = patch.patch.DeclaringType.Assembly.FullName;
            //                        string assname = ModInfoCache.AssemblyToModname[ass];

            //                        builder.AppendLine(
            //                            $"{patch.type} from {assname} with the index {patch.index} and the priority {patch.priority}\n");
            //                    }
            //                }

            //                TooltipHandler.TipRegion(rect, builder.ToString());
            //            }
            //        }
            //    }
        }
    }

}

