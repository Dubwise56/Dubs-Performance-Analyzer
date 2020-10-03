using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    public static class Panel_Details
    {
        private static GeneralInformation? currentInformation;

        public static void DrawMethDeets(Rect inrect)
        {
            Text.Font = GameFont.Tiny;

            if (GUIController.CurrentProfiler?.meth != null)
            {
                var currentMeth = GUIController.CurrentProfiler.meth;
                if (currentInformation == null || currentInformation.Value.method != currentMeth)
                {
                    GetGeneralSidePanelInformation();
                }

                var info = currentInformation.Value;

                var row = inrect;
                row.height = inrect.height / 3f;

                  Text.Anchor = TextAnchor.MiddleLeft;
                var str = $"{info.typeName}:{info.methodName}";

                Widgets.Label(row, str);

                Widgets.DrawHighlightIfMouseover(row);
                if (Widgets.ButtonInvisible(row)) // mouse button right
                {
                    var options = new List<FloatMenuOption>
                    {
                        new FloatMenuOption("Open In Github", () => OpenGithub($"{info.typeName}.{info.methodName}")),
                        new FloatMenuOption("Open In Dnspy (requires local path)", () => OpenDnspy(info.method))
                    };

                    Find.WindowStack.Add(new FloatMenu(options));
                }

                row.y = row.yMax;
                Widgets.Label(row, $"Mod: {info.modName}");
                row.y = row.yMax;
                Widgets.Label(row, $"Assembly: {info.assname.Split(',').First()}.dll");
                DubGUI.ResetFont();
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
                currentInformation = info;
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

            currentInformation = info;
        }

        private static void OpenGithub(string fullMethodName)
        {
            Application.OpenURL(@"https://github.com/search?l=C%23&q=" + fullMethodName + "&type=Code");
        }

        private static void OpenDnspy(MethodBase method)
        {
            if (string.IsNullOrEmpty(Settings.PathToDnspy))
            {
                Log.ErrorOnce("You have not given a local path to dnspy", 10293838);
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

        public static void DrawStats(Rect inrect)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            var s = new LogStats();
            s.GenerateStats();
            lock (CurrentLogStats.sync)
            {
                var st = CurrentLogStats.stats;
                var sb = new StringBuilder();
                sb.AppendLine($"Total Entries: {st.Entries}");
                sb.AppendLine($"Total Calls: {st.TotalCalls}");
                sb.AppendLine($"Total Time: {st.TotalTime:0.000}ms");
                sb.AppendLine($"Avg Time/Call: {st.MeanTimePerCall:0.000}ms");
                sb.AppendLine($"Avg Calls/Update: {st.MeanCallsPerUpdateCycle:0.00}");
                sb.AppendLine($"Avg Time/Update: {st.MeanTimePerUpdateCycle:0.000}ms");
                sb.AppendLine($"Median Calls: {st.MedianCalls}");
                sb.AppendLine($"Median Time: {st.MedianTime}");
                sb.AppendLine($"Max Time: {st.HighestTime:0.000}ms");
                sb.AppendLine($"Max Calls/Frame: {st.HighestCalls}");

                Widgets.Label(inrect, sb.ToString().TrimEndNewlines());
            }

            DubGUI.ResetFont();
        }
    }
}