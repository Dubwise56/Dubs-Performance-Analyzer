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

        private const float boxHeight = 40f;
        private const float labelOffset = 60f;

        private static Vector2 ScrollPosition = Vector2.zero;
        public static float cachedListHeight = float.MaxValue;

        public static string tipCache = "";
        public static string tipLabelCache = "";

        private static void DrawLogs(Rect rect)
        {
            if (!GUIController.CurrentEntry.isPatched)
            {
                DubGUI.Heading(rect, $"Loading{GenText.MarchingEllipsis(0f)}");
                return;
            }

            Rect innerRect = rect.AtZero();
            innerRect.height = cachedListHeight;

            viewFrustum = rect.AtZero(); // Our view frustum starts at 0,0 from the rect we are given
            viewFrustum.y += ScrollPosition.y; // adjust our view frustum vertically based on the scroll position

            { // Begin scope for Scroll View
                Widgets.BeginScrollView(rect, ref ScrollPosition, innerRect, false);
                GUI.BeginGroup(innerRect);
                listing.Begin(innerRect);

                float currentListHeight = 0;

                Text.Anchor = TextAnchor.MiddleLeft;
                Text.Font = GameFont.Tiny;

                lock (Analyzer.LogicLock)
                {
                    foreach (ProfileLog log in Analyzer.Logs)
                        DrawLog(log, ref currentListHeight);
                }

                cachedListHeight = currentListHeight;

                listing.End();
                GUI.EndGroup();
                Widgets.EndScrollView();
            }

            DubGUI.ResetFont();
        }

        private static void DrawLog(ProfileLog log, ref float currentListHeight)
        {
            Rect visible = listing.GetRect(boxHeight);

            if (!visible.Overlaps(viewFrustum)) // if we don't overlap, continue, but continue to adjust for further logs.
            {
                listing.GapLine(0f);
                currentListHeight += (boxHeight + 4);

                return;
            }

            Profiler profile = ProfileController.Profiles[log.key];

            Widgets.DrawHighlightIfMouseover(visible);

            if (GUIController.GetCurrentProfiler.key == profile.key)
                Widgets.DrawHighlightSelected(visible);

            // onhover tooltip
            if (Mouse.IsOver(visible))
                DrawHover(log, visible);

            // onclick work, left click view stats, right click internal patch, ctrl + left click unpatch
            if (Widgets.ButtonInvisible(visible))
                ClickWork(log, profile);

            // Colour a fillable bar below the log depending on the % fill of a log

            if (log.average <= .25f) // 25% or less
                Widgets.FillableBar(visible.BottomPartPixels(8f), log.percent, ResourceCache.GUI.grey, ResourceCache.GUI.clear, false);
            else if (log.average <= .75f) // between 25-75%
                Widgets.FillableBar(visible.BottomPartPixels(8f), log.percent, ResourceCache.GUI.blue, ResourceCache.GUI.clear, false);
            else // >= 75%
                Widgets.FillableBar(visible.BottomPartPixels(8f), log.percent, ResourceCache.GUI.red, ResourceCache.GUI.clear, false);


            // todo swap this value to things like average
            string timeLabel = $" {log.max:0000}ms";
            Widgets.Label(visible.LeftPartPixels(timeLabel.GetWidthCached()), timeLabel);

            // Line all of our labels up
            visible.x += labelOffset;
            visible.width = float.MaxValue; // make sure we don't see log labels spill over multiple 'lines'

            Widgets.Label(visible, log.label); // display our label

            GUI.color = Color.white;

            listing.GapLine(0f);
            currentListHeight += (boxHeight + 4);
        }

        public static void DrawHover(ProfileLog log, Rect visible)
        {
            if (log.meth != null)
            {
                if (log.label != tipLabelCache) // If we have a new label, re-create the string, else use our cached version.
                {
                    tipLabelCache = log.label;
                    StringBuilder builder = new StringBuilder();
                    Patches patches = Harmony.GetPatchInfo(log.meth);
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
                                string modName = ModInfoCache.AssemblyToModname[ass];

                                builder.AppendLine($"{type} from {modName} with the index {patch.index} and the priority {patch.priority}\n");
                            }
                        }

                        tipCache = builder.ToString();
                    }
                }
                TooltipHandler.TipRegion(visible, tipCache);
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
                    // This is now the active log 
                    GUIController.GetCurrentProfiler = profile;
                }
            }
            else if (Event.current.button == 1) // right click
            {
                if(log.meth == null) return; 

                List<FloatMenuOption> options = RightClickDropDown(log.meth as MethodInfo).ToList();
                Find.WindowStack.Add(new FloatMenu(options));
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
