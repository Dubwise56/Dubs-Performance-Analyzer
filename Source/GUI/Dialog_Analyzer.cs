using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;
using Verse;

/*  Naming Wise
 *  Tabs on the side, Ex 'HarmonyPatches', SideTab
 *  Categories for them, Ex 'Tick', SideTabCategories
 *  A Log inside a SideTab, is a 'Log', each Log belongs to a SideTab
 */

namespace DubsAnalyzer
{
    public enum CurrentState
    {
        Unitialised, Patching, Open, UnpatchingQueued, Unpatching
    }

    public enum CurrentSideTab
    {
        Home, ModderTools, Categories
    }

    [StaticConstructorOnStartup]
    public class Dialog_Analyzer : Window
    {
        public override Vector2 InitialSize => new Vector2(850, 650);
        private const float boxHeight = 40f;
        public static float patchListWidth = 220f;
        public static float yOffset = 0f;
        private float ListHeight = 0;
        public static Listing_Standard listing = new Listing_Standard();

        private static Vector2 ScrollPosition = Vector2.zero;
        private static Vector2 ScrollPosition_TabList = Vector2.zero;

        public static string GarbageCollectionInfo = string.Empty;

        public static string TimesFilter = string.Empty;
        public static StringBuilder csv = new StringBuilder();
        public Rect GizmoListRect;


        public static long totalBytesOfMemoryUsed;

        public static CurrentSideTab CurrentSideTab = CurrentSideTab.Home;
        public static CurrentState State = CurrentState.Unitialised;
        public static string CurrentKey = string.Empty;
        private static Thread CleanupPatches = null;

        public override void PreOpen()
        {
            base.PreOpen();
            if (State == CurrentState.Unitialised)
            {
                State = CurrentState.Patching;
                Log.Message("Applying profiling patches...");
                try
                {
                    var modes = GenTypes.AllTypes.Where(m => m.TryGetAttribute<ProfileMode>(out _)).OrderBy(m => m.TryGetAttribute<ProfileMode>().name);

                    foreach (var mode in modes)
                    {
                        try
                        {
                            var att = mode.TryGetAttribute<ProfileMode>();
                            att.Settings = new Dictionary<FieldInfo, Setting>();

                            foreach (var fieldInfo in mode.GetFields().Where(m => m.TryGetAttribute<Setting>(out _)))
                            {
                                var sett = fieldInfo.TryGetAttribute<Setting>();
                                att.Settings.SetOrAdd(fieldInfo, sett);
                            }
                            att.MouseOver = AccessTools.Method(mode, "MouseOver");
                            att.Clicked = AccessTools.Method(mode, "Clicked");
                            att.Selected = AccessTools.Method(mode, "Selected");
                            att.Checkbox = AccessTools.Method(mode, "Checkbox");
                            att.typeRef = mode;

                            foreach (var profileTab in SideTabCategories)
                            {
                                if (att.mode == profileTab.UpdateMode)
                                    profileTab.Modes.SetOrAdd(att, mode);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Error(e.ToString());
                        }
                    }
                    try
                    {
                        Analyzer.harmony.PatchAll(Assembly.GetExecutingAssembly());
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.ToString());
                    }

                    Log.Message("Done");
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }
            }
            State = CurrentState.Open;
            Analyzer.StartProfiling();
        }

        public override void PostClose()
        {
            base.PostClose();
            Analyzer.StopProfiling();
            Analyzer.Reset();
            Analyzer.Settings.Write();

            // we add new functionality
            if (State != CurrentState.Unitialised)
            {
                CleanupPatches = new Thread(() => Analyzer.unPatchMethods());
                CleanupPatches.Start();
            }
        }

        public static List<ProfileTab> SideTabCategories = new List<ProfileTab>
        {
            new ProfileTab("Home",          () => { CurrentSideTab = CurrentSideTab.Home; },         () => CurrentSideTab == CurrentSideTab.Home,            UpdateMode.Dead,    "Settings and utils"),
            new ProfileTab("Modder Tools",  () => { CurrentSideTab = CurrentSideTab.ModderTools; },  () => CurrentSideTab == CurrentSideTab.ModderTools,     UpdateMode.Dead,    "Utilities for modders and advanced users to profile mods!"),
            new ProfileTab("Tick",          () => { CurrentSideTab = CurrentSideTab.Categories; },   () => false,                                            UpdateMode.Tick,    "Things that run on tick"),
            new ProfileTab("Update",        () => { CurrentSideTab = CurrentSideTab.Categories; },   () => false,                                            UpdateMode.Update,  "Things that run per frame"),
            new ProfileTab("GUI",           () => { CurrentSideTab = CurrentSideTab.Categories; },   () => false,                                            UpdateMode.GUI,      "Things that run on GUI")
            // we don't actually ever want the 'category' tabs (currently) to be shown as selected, because there is (currently) nothing to be shown if it were to be selected
        };

        public Dialog_Analyzer()
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
        }


        public override void SetInitialSizeAndPosition()
        {
            windowRect = new Rect(50f, (UI.screenHeight - InitialSize.y) / 2f, InitialSize.x, InitialSize.y);
            windowRect = windowRect.Rounded();
        }

        public static IEnumerable<MethodInfo> SearchFor()
        {
            foreach (var allType in GenTypes.AllTypes)
            {
                if (PerfAnalSettings.TypeSearch == string.Empty || allType.Name.Has(PerfAnalSettings.TypeSearch))
                {
                    foreach (var v in allType.GetMethods())
                    {
                        if (PerfAnalSettings.MethSearch == string.Empty || v.Name.Has(PerfAnalSettings.MethSearch))
                        {
                            yield return v;
                        }
                    }
                }
            }
        }


        public override void DoWindowContents(Rect canvas)
        {
            if (Event.current.type == EventType.Layout)
            {
                return;
            }

            var ListerBox = canvas.LeftPartPixels(patchListWidth);
            ListerBox.width -= 10f;
            Widgets.DrawMenuSection(ListerBox);
            ListerBox = ListerBox.ContractedBy(4f);

            var baseRect = ListerBox.AtZero();
            baseRect.width -= 16f;
            baseRect.height = ListHeight;

            { // Scope this for clarity
                Text.Anchor = TextAnchor.MiddleLeft;
                Text.Font = GameFont.Tiny;

                yOffset = 0f;

                Widgets.BeginScrollView(ListerBox, ref ScrollPosition_TabList, baseRect);
                GUI.BeginGroup(baseRect);
                listing.Begin(baseRect);

                foreach (var maintab in SideTabCategories)
                    DrawSideTabList(maintab);

                listing.End();
                GUI.EndGroup();
                Widgets.EndScrollView();


                ListHeight = yOffset;
                DubGUI.ResetFont();
            }

            var inner = canvas.RightPartPixels(canvas.width - patchListWidth).Rounded();

            if (CurrentSideTab == CurrentSideTab.Home)
            {
                Analyzer.Settings.DoSettings(inner);
                if (windowRect.width > InitialSize.x)
                    windowRect.width -= 450;
            }
            else if (CurrentSideTab == CurrentSideTab.ModderTools)
            {
                Dialog_ModdingTools.DoWindowContents(inner);
                if (windowRect.width > InitialSize.x)
                    windowRect.width -= 450;
            }
            else
            {
                try
                {
                    if (State == CurrentState.Open)
                    {
                        if (Dialog_Graph.key != string.Empty)
                        {
                            if (windowRect.width <= InitialSize.x)
                                windowRect.width += 450;

                            Rect blurg = inner;
                            blurg.width -= 450;
                            Widgets.DrawMenuSection(blurg);
                            blurg = blurg.ContractedBy(6f);
                            DoThingTab(blurg);

                            Rect r = new Rect(canvas.x + (windowRect.width - 478), canvas.y, 432, canvas.height);
                            Dialog_LogAdditional.DoWindowContents(r);
                        }
                        else
                        {
                            Widgets.DrawMenuSection(inner);
                            DoThingTab(inner.ContractedBy(6f));
                        }
                    }
                    else
                    {
                        Widgets.DrawMenuSection(inner);
                        Text.Font = GameFont.Medium;
                        Text.Anchor = TextAnchor.MiddleCenter;
                        Widgets.Label(inner, $"Loading{GenText.MarchingEllipsis(0f)}");
                        DubGUI.ResetFont();
                    }
                }
                catch (Exception) { }
            }
        }

        private void DoThingTab(Rect rect)
        {
            if (!Analyzer.SelectedMode.IsPatched)
            {
                DubGUI.Heading(rect, $"Loading{GenText.MarchingEllipsis(0f)}");
                return;
            }

            Rect topslot = rect.TopPartPixels(20f);

            bool save = false;

            DrawTopRow(ref rect, topslot, ref save);

            var innerRect = rect.AtZero();
            innerRect.width -= 16f;
            innerRect.height = yOffset;

            GizmoListRect = rect.AtZero();
            GizmoListRect.y += ScrollPosition.y;
            Widgets.BeginScrollView(rect, ref ScrollPosition, innerRect);

            GUI.BeginGroup(innerRect);

            listing.Begin(innerRect);

            float currentListHeight = 0;

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Tiny;

            lock (Analyzer.sync)
            {
                foreach (var log in Analyzer.Logs)
                {
                    DrawLog(log, save, ref currentListHeight);
                }
            }

            if (save)
            {
                var path = GenFilePaths.FolderUnderSaveData("Profiling") + $"/{Analyzer.SelectedMode.name}_{DateTime.Now.ToFileTime()}.csv";
                File.WriteAllText(path, csv.ToString());
                csv.Clear();
                Messages.Message($"Saved to {path}", MessageTypeDefOf.TaskCompletion, false);
            }

            listing.End();
            GUI.EndGroup();

            DubGUI.ResetFont();
            Widgets.EndScrollView();
        }

        private void DrawLog(ProfileLog log, bool save, ref float currentListHeight)
        {
            if (!log.Label.Has(TimesFilter)) return;

            Rect visible = listing.GetRect(boxHeight);

            if (visible.Overlaps(GizmoListRect))
            {
                var profile = Analyzer.Profiles[log.Key];

                if (Input.GetKey(KeyCode.LeftControl))
                {
                    if (Widgets.ButtonInvisible(visible))
                    {
                        Analyzer.SelectedMode.Clicked?.Invoke(null, new object[] { profile, log });
                        Analyzer.Settings.Write();
                    }
                }

                bool on = true;

                if (Analyzer.SelectedMode.Selected != null)
                    on = (bool)Analyzer.SelectedMode.Selected.Invoke(null, new object[] { profile, log });

                if (Analyzer.SelectedMode.Checkbox != null)
                {
                    var checkboxRect = new Rect(visible.x, visible.y, 25f, visible.height);
                    visible.x += 25f;
                    if (DubGUI.Checkbox(checkboxRect, "", ref on))
                    {
                        Analyzer.SelectedMode.Checkbox?.Invoke(null, new object[] { profile, log });
                        Analyzer.Settings.Write();
                    }
                }

                Widgets.DrawHighlightIfMouseover(visible);

                if (Widgets.ButtonInvisible(visible))
                {
                    Dialog_Graph.RunKey(log.Key);
                    CurrentKey = log.Key;
                }

                if (CurrentKey == log.Key)
                    Widgets.DrawHighlightSelected(visible);

                if (Mouse.IsOver(visible))
                    Analyzer.SelectedMode.MouseOver?.Invoke(null, new object[] { visible, profile, log });

                var color = DubResources.grey;
                if (log.Percent > 0.25f)
                    color = DubResources.blue;
                else if (log.Percent > 0.75f)
                    color = DubResources.red;

                Widgets.FillableBar(visible.BottomPartPixels(8f), log.Percent, color, DubResources.clear, false);

                visible = visible.LeftPartPixels(60);

                if (!on)
                    GUI.color = Color.grey;

                Widgets.Label(visible, $"{log.Max:0.000}ms");

                visible.x = visible.xMax + 15;

                visible.width = 2000;
                Widgets.Label(visible, log.Label);

                GUI.color = Color.white;

                if (save)
                {
                    foreach (var historyTime in profile.History.times)
                    {
                        csv.Append($",{historyTime}");
                    }
                    csv.AppendLine();
                }
            }

            listing.GapLine(0f);
            currentListHeight += 4f;
            currentListHeight += visible.height;
        }

        private void DrawTopRow(ref Rect rect, Rect topRow, ref bool save)
        {
            Rect row = topRow.LeftPartPixels(25f);

            if (Widgets.ButtonImage(row, TexButton.SpeedButtonTextures[Analyzer.running ? 0 : 1]))
                Analyzer.running = !Analyzer.running;

            TooltipHandler.TipRegion(topRow, "startstoplogTip".Translate());
            save = false;

            Rect searchbox = topRow.LeftPartPixels(topRow.width - 350f);
            searchbox.x += 25f;
            DubGUI.InputField(searchbox, "Search", ref TimesFilter, DubGUI.MintSearch);
            row.x = searchbox.xMax + 5;
            row.width = 130f;
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            Widgets.FillableBar(row, Mathf.Clamp01(Mathf.InverseLerp(H_RootUpdate.LastMinGC, H_RootUpdate.LastMaxGC, totalBytesOfMemoryUsed)), DubResources.darkgrey);
            Widgets.Label(row, GarbageCollectionInfo);
            Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(row, "garbageTip".Translate());

            row.x = row.xMax + 5;
            row.width = 50f;
            Widgets.Label(row, H_RootUpdate._fpsText);
            TooltipHandler.TipRegion(row, "fpsTipperino".Translate());
            row.x = row.xMax + 5;
            row.width = 90f;
            Widgets.Label(row, H_RootUpdate.tps);
            TooltipHandler.TipRegion(row, "tpsTipperino".Translate());
            row.x = row.xMax + 5;
            row.width = 30f;
            Text.Font = GameFont.Medium;
            save = Widgets.ButtonImageFitted(row, DubResources.sav);
            TooltipHandler.TipRegion(row, "savecsvTip".Translate(GenFilePaths.FolderUnderSaveData("Profiling")));

            row.x = row.xMax;
            row.width = 25f;

            rect.y += 25f;
            rect.height -= 25f;
        }

        private void DrawSideTabList(ProfileTab tab)
        {
            DubGUI.ResetFont();
            yOffset += 40f;
            var row = listing.GetRect(30f);

            if (tab.Selected) Widgets.DrawOptionSelected(row);
            if (Widgets.ButtonInvisible(row)) tab.clickedAction();

            row.x += 5f;
            Widgets.Label(row, tab.label);

            TooltipHandler.TipRegion(row, tab.Tip);

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Tiny;

            foreach (var mode in tab.Modes)
            {
                DrawSideTab(ref row, mode);
            }
        }

        private void DrawSideTab(ref Rect row, KeyValuePair<ProfileMode, Type> mode)
        {
            if (!mode.Key.Basics && !Analyzer.Settings.AdvancedMode) return;

            row = listing.GetRect(30f);
            Widgets.DrawHighlightIfMouseover(row);

            if (Analyzer.SelectedMode == mode.Key) Widgets.DrawOptionSelected(row);

            row.x += 20f;
            yOffset += 30f;

            Widgets.Label(row, mode.Key.name);

            if (Widgets.ButtonInvisible(row))
            {
                if (!(CurrentSideTab == CurrentSideTab.Categories))
                {
                    CurrentSideTab = CurrentSideTab.Categories;
                    Analyzer.Settings.Write();
                }
                if (Analyzer.SelectedMode != null)
                    AccessTools.Field(Analyzer.SelectedMode.typeRef, "Active").SetValue(null, false);

                AccessTools.Field(mode.Value, "Active").SetValue(null, true);
                Analyzer.SelectedMode = mode.Key;
                Analyzer.Reset();

                if (!mode.Key.IsPatched)
                    mode.Key.ProfilePatch();
            }

            TooltipHandler.TipRegion(row, mode.Key.tip);

            if (Analyzer.SelectedMode == mode.Key)
            {
                bool firstEntry = true;
                foreach (var keySetting in mode.Key.Settings)
                {
                    if (keySetting.Key.FieldType == typeof(bool))
                    {
                        row = listing.GetRect(30f);
                        row.x += 20f;
                        GUI.color = Widgets.OptionSelectedBGBorderColor;
                        Widgets.DrawLineVertical(row.x, row.y, 15f);

                        if (firstEntry)
                        {
                            Widgets.DrawLineVertical(row.x, row.y - 15f, 15f);
                        }

                        row.x += 10f;
                        Widgets.DrawLineHorizontal(row.x - 10f, row.y + 15f, 10f);
                        GUI.color = Color.white;
                        yOffset += 30f;

                        bool cur = (bool)keySetting.Key.GetValue(null);

                        if (DubGUI.Checkbox(row, keySetting.Value.name, ref cur))
                        {
                            keySetting.Key.SetValue(null, cur);
                            Analyzer.Reset();
                        }
                    }

                    if (keySetting.Value.tip != null)
                    {
                        TooltipHandler.TipRegion(row, keySetting.Value.tip);
                    }

                    firstEntry = false;
                }
            }
        }
    }
}