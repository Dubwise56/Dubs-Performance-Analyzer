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
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.UIElements;
using Verse;

/*  Naming Wise
 *  Tabs on the side, Ex 'HarmonyPatches', SideTab
 *  Categories for them, Ex 'Tick', SideTabCategories
 *  A Log 'inside' a SideTab, is a 'Log', each Log belongs to a SideTab
 */

namespace DubsAnalyzer
{

    [StaticConstructorOnStartup]
    public class Dialog_Analyzer : Window
    {
        public override Vector2 InitialSize => new Vector2(850, 650);
        public Vector2 GraphSize = new Vector2(1300, 650);

        private SideTab sideTab;

        private const float boxHeight = 40f;
        public static float yOffset = 0f;
        private float ListHeight = 0;
        public static Listing_Standard listing = new Listing_Standard();

        private static Vector2 ScrollPosition = Vector2.zero;

        public static string GarbageCollectionInfo = string.Empty;

        public static string TimesFilter = string.Empty;
        public static StringBuilder csv = new StringBuilder();
        public Rect GizmoListRect;

        public static long totalBytesOfMemoryUsed;

        private static Thread CleanupPatches = null;
        public static List<Action> QueuedMessages = new List<Action>();

        public override void PreOpen()
        {
            base.PreOpen();
            if (AnalyzerState.State == CurrentState.Unitialised)
            {
                AnalyzerState.State = CurrentState.Patching;
                Log.Message("Applying profiling patches...");
                try
                {
                    var modes = GenTypes.AllTypes.Where(m => m.TryGetAttribute<ProfileMode>(out _)).OrderBy(m => m.TryGetAttribute<ProfileMode>().name).ToList();

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

                            foreach (var profileTab in AnalyzerState.SideTabCategories)
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


                    foreach (var profileMode in ProfileMode.instances)
                    {
                        foreach (var profileTab in AnalyzerState.SideTabCategories)
                        {
                            if (profileMode.mode == profileTab.UpdateMode)
                            {
                                if (profileTab.Modes.Keys.All(x => x.name != profileMode.name))
                                {
                                    profileTab.Modes.Add(profileMode, null);
                                }
                            }
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
            AnalyzerState.State = CurrentState.Open;
            Analyzer.StartProfiling();
        }

        public override void PostClose()
        {
            base.PostClose();
            Analyzer.StopProfiling();
            Analyzer.Reset();
            Analyzer.Settings.Write();

            // we add new functionality
            if (AnalyzerState.CanCleanup())
            {
                CleanupPatches = new Thread(() => Analyzer.unPatchMethods());
                CleanupPatches.Start();
            }
        }


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

            sideTab = new SideTab(this);
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
            if (Event.current.type == EventType.Layout) return;

            /*
             * Draw our side tab, including our:
             * - Categories (Home, Modding Tools, Tick, Update, GUI)
             * - Content (Patches inside each of the above categories)
             */
            sideTab.Draw(canvas);

            /*
             * Draw the actual screen we want, either:
             * - Home Screen, Modders Tools, or one of the categories
             */
            Rect inner = canvas.RightPartPixels(canvas.width - SideTab.width).Rounded();

            switch (AnalyzerState.CurrentSideTabCategory)
            {
                case SideTabCategory.Home:
                    Analyzer.Settings.DoSettings(inner);
                    break;
                case SideTabCategory.ModderTools:
                    Dialog_ModdingTools.DoWindowContents(inner);
                    break;
                default: // We are in one of our categories, which means we want to display our logs

                    break;
            }

            // Now we are outside the scope of all of our gui, lets print the messages we had queued during this time
            foreach (var action in QueuedMessages)
                action();

            QueuedMessages.Clear();
        }

        //try
        //            {
        //                if (AnalyzerState.State == CurrentState.Open)
        //                {
        //                    if (Dialog_Graph.key != string.Empty || AnalyzerState.CurrentProfileKey == "Overview")
        //                    {
        //                        windowRect.width = GraphSize.x;

        //                        Rect innerLogRect = inner;
        //innerLogRect.width -= 450;
        //                        Widgets.DrawMenuSection(innerLogRect);
        //                        innerLogRect = innerLogRect.ContractedBy(6f);
        //                        DrawLogs(innerLogRect);

        //var size = inner.x + inner.width;
        //Rect r = new Rect(size, canvas.y, windowRect.width - size, canvas.height);
        //                        if (AnalyzerState.CurrentProfileKey == "Overview")
        //                        {
        //                            Dialog_StackedGraph.Display(r);
        //                        }
        //                        else
        //                        {
        //                            Dialog_LogAdditional.DoWindowContents(r);
        //                            GUI.EndGroup();
        //                        }
        //                    }
        //                    else
        //                    {
        //                        Widgets.DrawMenuSection(inner);
        //                        DrawLogs(inner.ContractedBy(6f));
        //                    }
        //                }
        //                else
        //                {
        //                    Widgets.DrawMenuSection(inner);
        //                    Text.Font = GameFont.Medium;
        //                    Text.Anchor = TextAnchor.MiddleCenter;
        //                    Widgets.Label(inner, $"Loading{GenText.MarchingEllipsis(0f)}");
        //                    DubGUI.ResetFont();
        //                }
        //            }
        //            catch (Exception) { }


        private void DrawLogs(Rect rect)
        {
            if (!AnalyzerState.CurrentTab.IsPatched)
            {
                DubGUI.Heading(rect, $"Loading{GenText.MarchingEllipsis(0f)}");
                return;
            }

            Rect topslot = rect.TopPartPixels(20f);

            bool save = false;

            DrawTopRow(topslot, ref save);
            rect.y += 25f;
            rect.height -= 25f;

            var innerRect = rect.AtZero();
            innerRect.width -= 16f;
            innerRect.height = yOffset;

            GizmoListRect = rect.AtZero();
            GizmoListRect.y += ScrollPosition.y;

            GUI.BeginGroup(innerRect);
            Widgets.BeginScrollView(rect, ref ScrollPosition, innerRect);
            listing.Begin(innerRect);

            float currentListHeight = 0;

            // Lets have a 'tab' summary 
            // We will get stats like a; total time on tab
            Rect visible = listing.GetRect(20);

            Text.Anchor = TextAnchor.MiddleCenter;
            DrawTabOverview(visible);
            currentListHeight += 24;
            listing.GapLine(0f);

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Tiny;

            lock (Analyzer.sync)
            {
                foreach (var log in AnalyzerState.Logs)
                {
                    DrawLog(log, save, ref currentListHeight);
                }
            }

            if (save)
            {
                var path = GenFilePaths.FolderUnderSaveData("Profiling") + $"/{AnalyzerState.CurrentTab.name}_{DateTime.Now.ToFileTime()}.csv";
                File.WriteAllText(path, csv.ToString());
                csv.Clear();
                Messages.Message($"Saved to {path}", MessageTypeDefOf.TaskCompletion, false);
            }

            listing.End();
            Widgets.EndScrollView();
            GUI.EndGroup();

            DubGUI.ResetFont();
        }

        private void DrawTabOverview(Rect rect)
        {

            Widgets.Label(rect, AnalyzerState.CurrentTab.name);

            Widgets.DrawHighlightIfMouseover(rect);

            if (Widgets.ButtonInvisible(rect))
                AnalyzerState.CurrentProfileKey = "Overview";

            if (AnalyzerState.CurrentProfileKey == "Overview")
                Widgets.DrawHighlightSelected(rect);
        }

        private void DrawLog(ProfileLog log, bool save, ref float currentListHeight)
        {
            if (!log.Label.Has(TimesFilter)) return;

            Rect visible = listing.GetRect(boxHeight);

            if (visible.Overlaps(GizmoListRect))
            {
                var profile = AnalyzerState.GetProfile(log.Key);

                if (Input.GetKey(KeyCode.LeftControl))
                {
                    if (Widgets.ButtonInvisible(visible))
                    {
                        AnalyzerState.CurrentTab.Clicked?.Invoke(null, new object[] { profile, log });
                        Analyzer.Settings.Write();
                    }
                }

                bool on = true;

                if (AnalyzerState.CurrentTab.Selected != null)
                    on = (bool)AnalyzerState.CurrentTab.Selected.Invoke(null, new object[] { profile, log });

                if (AnalyzerState.CurrentTab.Checkbox != null)
                {
                    var checkboxRect = new Rect(visible.x, visible.y, 25f, visible.height);
                    visible.x += 25f;
                    if (DubGUI.Checkbox(checkboxRect, "", ref on))
                    {
                        AnalyzerState.CurrentTab.Checkbox?.Invoke(null, new object[] { profile, log });
                        Analyzer.Settings.Write();
                    }
                }

                Widgets.DrawHighlightIfMouseover(visible);

                if (Widgets.ButtonInvisible(visible))
                {
                    Dialog_Graph.RunKey(log.Key);
                    AnalyzerState.CurrentProfileKey = log.Key;
                }

                if (AnalyzerState.CurrentProfileKey == log.Key)
                    Widgets.DrawHighlightSelected(visible);

                if (Mouse.IsOver(visible))
                    AnalyzerState.CurrentTab.MouseOver?.Invoke(null, new object[] { visible, profile, log });

                if (Input.GetMouseButtonDown(1)) // mouse button right
                {
                    if (visible.Contains(Event.current.mousePosition))
                    {
                        if (log.Meth != null)
                        {
                            List<FloatMenuOption> options = RightClickDropDown(log.Meth).ToList();
                            Find.WindowStack.Add(new FloatMenu(options));
                        }
                        else
                        {
                            try
                            {
                                var methnames = PatchUtils.GetSplitString(log.Key);
                                foreach (var n in methnames)
                                {
                                    var meth = AccessTools.Method(n);
                                    List<FloatMenuOption> options = RightClickDropDown(meth).ToList();
                                    Find.WindowStack.Add(new FloatMenu(options));
                                }
                            }
                            catch (Exception) { }
                        }
                    }
                }


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

        private static IEnumerable<FloatMenuOption> RightClickDropDown(MethodInfo meth)
        {
            if (Analyzer.Settings.AdvancedMode)
            {
                if (AnalyzerState.CurrentProfileKey.Contains("Harmony")) // we can return an 'unpatch'
                {
                    yield return new FloatMenuOption("Unpatch Method", delegate
                        {
                            PatchUtils.UnpatchMethod(meth);
                        });
                }

                yield return new FloatMenuOption("Unpatch methods that patch", delegate
                    {
                        PatchUtils.UnpatchMethodsOnMethod(meth);
                    });

                yield return new FloatMenuOption("Profile the internal methods of", delegate
                    {
                        PatchUtils.PatchInternalMethod(meth);
                    });
            }
        }

        private void DrawTopRow(Rect topRow, ref bool save)
        {
            Rect row = topRow.LeftPartPixels(25f);

            if (Widgets.ButtonImage(row, TexButton.SpeedButtonTextures[AnalyzerState.CurrentlyRunning ? 0 : 1]))
                AnalyzerState.CurrentlyRunning = !AnalyzerState.CurrentlyRunning;

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
        }


        internal class SideTab
        {
            private Dialog_Analyzer super = null;
            public static float width = 220f;
            private static Vector2 ScrollPosition = Vector2.zero;
            public SideTab(Dialog_Analyzer super)
            {
                this.super = super;
            }

            public void Draw(Rect rect)
            {
                var ListerBox = rect.LeftPartPixels(width);
                ListerBox.width -= 10f;
                Widgets.DrawMenuSection(ListerBox);
                ListerBox = ListerBox.ContractedBy(4f);

                var baseRect = ListerBox.AtZero();
                baseRect.width -= 16f;
                baseRect.height = super.ListHeight;

                Text.Anchor = TextAnchor.MiddleLeft;
                Text.Font = GameFont.Tiny;

                yOffset = 0f;

                { // Begin Scope for Scroll & GUI Group/View
                    Widgets.BeginScrollView(ListerBox, ref ScrollPosition, baseRect);
                    GUI.BeginGroup(baseRect);
                    listing.Begin(baseRect);

                    foreach (var maintab in AnalyzerState.SideTabCategories)
                        DrawSideTabList(maintab);

                    listing.End();
                    GUI.EndGroup();
                    Widgets.EndScrollView();
                }


                DubGUI.ResetFont();
                super.ListHeight = yOffset;
            }

            private void DrawSideTabList(ProfileTab tab)
            {
                DubGUI.ResetFont();
                yOffset += 40f;

                var row = listing.GetRect(30f);

                if (tab.Selected) Widgets.DrawOptionSelected(row);
                if (tab.label == "Home" || tab.label == "Modder Tools")
                {
                    if (Widgets.ButtonInvisible(row))
                        tab.clickedAction();
                }
                else
                {
                    if (Widgets.ButtonInvisible(row.LeftPart(0.9f)))
                        tab.clickedAction();
                    if (Widgets.ButtonImage(row.RightPart(0.1f).ContractedBy(1f), tab.Collapsed ? TexButton.Reveal : TexButton.Collapse))
                        tab.Collapsed = !tab.Collapsed;
                }
                row.x += 5f;
                Widgets.Label(row, tab.label);

                TooltipHandler.TipRegion(row, tab.Tip);

                Text.Anchor = TextAnchor.MiddleLeft;
                Text.Font = GameFont.Tiny;

                if (tab.Collapsed) return;

                foreach (var mode in tab.Modes)
                {
                    DrawSideTab(ref row, mode, tab.UpdateMode);
                }
            }

            private void DrawSideTab(ref Rect row, KeyValuePair<ProfileMode, Type> mode, UpdateMode updateMode)
            {
                if (!mode.Key.Basics && !Analyzer.Settings.AdvancedMode) return;

                row = listing.GetRect(30f);
                Widgets.DrawHighlightIfMouseover(row);

                if (AnalyzerState.CurrentTab == mode.Key)
                    Widgets.DrawOptionSelected(row);

                row.x += 20f;
                yOffset += 30f;

                Widgets.Label(row, mode.Key.name);

                if (Widgets.ButtonInvisible(row))
                {
                    AnalyzerState.SwapTab(mode, updateMode);
                }

                TooltipHandler.TipRegion(row, mode.Key.tip);

                if (AnalyzerState.CurrentTab == mode.Key)
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

                            if (!firstEntry)
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
        internal class Logs
        {
            private Dialog_Analyzer super = null;
            private static Vector2 ScrollPosition = Vector2.zero;
            public Logs(Dialog_Analyzer super)
            {
                this.super = super;
            }
        }
    }
}