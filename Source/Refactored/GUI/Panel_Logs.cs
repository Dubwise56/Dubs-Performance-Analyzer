using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Analyzer
{
    public static class Panel_Logs
    {
        private static Listing_Standard listing = new Listing_Standard();
        private static Rect viewFrustum;
        private static Vector2 ScrollPosition = Vector2.zero;
        private const float boxHeight = 40f;
        public static float ListHeight = 999999999;
        public static float width = 630f;

        public static string TipCache = "";
        public static string TipLabel = "";

        private static void DrawLogs(Rect rect, bool save)
        {
            if (!GUIController.CurrentEntry.isPatched)
            {

                DubGUI.Heading(rect, $"Loading{GenText.MarchingEllipsis(0f)}");
                return;
            }

            Rect innerRect = rect.AtZero();
            //innerRect.width -= 16f;
            innerRect.height = ListHeight;

            viewFrustum = rect.AtZero();
            viewFrustum.y += ScrollPosition.y;

            Widgets.BeginScrollView(rect, ref ScrollPosition, innerRect, false);
            GUI.BeginGroup(innerRect);
            listing.Begin(innerRect);

            float currentListHeight = 0;

            // Lets have a 'tab' summary 
            // We will get stats like a; total time on tab
            Rect visible = listing.GetRect(20);

            Text.Anchor = TextAnchor.MiddleCenter;
            currentListHeight += 24;
            listing.GapLine(0f);

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Tiny;

            lock (Analyzer.LogicLock)
            {
                foreach (ProfileLog log in Analyzer.Logs)
                {
                    DrawLog(log, save, ref currentListHeight);
                }
            }

            ListHeight = currentListHeight;

            listing.End();
            GUI.EndGroup();
            Widgets.EndScrollView();

            DubGUI.ResetFont();
        }

        private static void DrawLog(ProfileLog log, bool save, ref float currentListHeight)
        {
            Rect visible = listing.GetRect(boxHeight);

            if (!visible.Overlaps(viewFrustum)) // if we don't overlap, continue, but continue to adjust for further logs.
            {
                listing.GapLine(0f);
                currentListHeight += 4f;
                currentListHeight += visible.height;

                return;
            }

            Profiler profile = Analyzer.profiles[log.Key];
            Entry currentEntry = GUIController.CurrentEntry;

            bool currentlyActive = true;

            if (currentEntry.onSelect != null)
            {
                currentlyActive = (bool)currentEntry.onSelect.Invoke(null, new object[] { profile, log });
            }

            //if (currentEntry.on != null)
            //{
            //    Rect checkboxRect = new Rect(visible.x, visible.y, 25f, visible.height);
            //    visible.x += 25f;
            //    if (DubGUI.Checkbox(checkboxRect, "", ref currentlyActive))
            //    {
            //        AnalyzerState.CurrentTab.Checkbox?.Invoke(null, new object[] { profile, log });
            //        Modbase.Settings.Write();
            //    }
            //}

            Widgets.DrawHighlightIfMouseover(visible);


            //if (AnalyzerState.CurrentProfileKey == log.Key)
            //{
            //    Widgets.DrawHighlightSelected(visible);
            //    AnalyzerState.CurrentLog = log; // because we create new ones, instead of recycle the same log, we need to update the ref.
            //}

            // onhover tooltip
            if (Mouse.IsOver(visible)) DrawHover(log, visible);

            // onclick work, left click view stats, right click internal patch, ctrl + left click unpatch
            if (Widgets.ButtonInvisible(visible)) ClickWork(log, profile);

            // draw the bar
            {
                Texture2D color = DubResources.grey;

                if (log.Percent > 0.25f) color = DubResources.blue;
                else if (log.Percent > 0.75f) color = DubResources.red;

                Widgets.FillableBar(visible.BottomPartPixels(8f), log.Percent, color, DubResources.clear, false);
            }
            visible = visible.LeftPartPixels(60);


            if (!currentlyActive) GUI.color = Color.grey;

            Widgets.Label(visible, $" {log.Max:0.000}ms");

            visible.x = visible.xMax + 15;

            visible.width = 2000;
            Widgets.Label(visible, log.Label);

            GUI.color = Color.white;

            listing.GapLine(0f);
            currentListHeight += 4f;
            currentListHeight += visible.height;
        }

        public static void DrawHover(ProfileLog log, Rect visible)
        {
            if (log.Meth != null)
            {
                if (log.Label != TipLabel)
                {
                    TipLabel = log.Label;
                    StringBuilder builder = new StringBuilder();
                    Patches patches = Harmony.GetPatchInfo(log.Meth);
                    if (patches != null)
                    {
                        foreach (Patch patch in patches.Prefixes) GetString("Prefix", patch);
                        foreach (Patch patch in patches.Postfixes) GetString("Postfix", patch);
                        foreach (Patch patch in patches.Transpilers) GetString("Transpiler", patch);
                        foreach (Patch patch in patches.Finalizers) GetString("Finalizer", patch);

                        void GetString(string type, Patch patch)
                        {
                            if (patch.owner != Modbase.Harmony.Id && patch.owner != InternalMethodUtility.Harmony.Id)
                            {
                                string ass = patch.PatchMethod.DeclaringType.Assembly.FullName;
                                string assname = ModInfoCache.AssemblyToModname[ass];

                                builder.AppendLine($"{type} from {assname} with the index {patch.index} and the priority {patch.priority}\n");
                            }
                        }

                        TipCache = builder.ToString();
                    }
                }
                TooltipHandler.TipRegion(visible, TipCache);
            }
        }
        public static void ClickWork(ProfileLog log, Profiler profile)
        {
            if (Event.current.button == 0) // left click
            {
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    GUIController.CurrentEntry.onClick?.Invoke(null, new object[] { profile, log });
                    Modbase.Settings.Write();
                }
                else
                {
                    // This should now be the active log 
                }
            }
            else if (Event.current.button == 1) // right click
            {
                if (log.Meth != null)
                {
                    List<FloatMenuOption> options = RightClickDropDown(log.Meth as MethodInfo).ToList();
                    Find.WindowStack.Add(new FloatMenu(options));
                }
                else
                {
                    try
                    {
                        IEnumerable<string> methnames = Utility.GetSplitString(log.Key);
                        foreach (string n in methnames)
                        {
                            MethodInfo meth = AccessTools.Method(n);
                            List<FloatMenuOption> options = RightClickDropDown(meth).ToList();
                            Find.WindowStack.Add(new FloatMenu(options));
                        }
                    }
                    catch (Exception) { }
                }
            }
        }
        private static IEnumerable<FloatMenuOption> RightClickDropDown(MethodInfo meth)
        {

            if (GUIController.GetCurrentProfiler.label.Contains("Harmony")) // we can return an 'unpatch'
                yield return new FloatMenuOption("Unpatch Method", () => Utility.UnpatchMethod(meth));

            yield return new FloatMenuOption("Unpatch methods that patch", () => Utility.UnpatchMethodsOnMethod(meth));
            yield return new FloatMenuOption("Profile the internal methods of", () => Utility.PatchInternalMethod(meth));
        }
    }
}
