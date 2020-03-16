

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;


namespace DubsAnalyzer
{
    [StaticConstructorOnStartup]
    public class Dialog_Analyzer : Window
    {
        public static readonly Texture2D black = SolidColorMaterials.NewSolidColorTexture(Color.black);
        public static readonly Texture2D grey = SolidColorMaterials.NewSolidColorTexture(Color.grey);
        public static readonly Texture2D clear = SolidColorMaterials.NewSolidColorTexture(Color.clear);
        public static readonly Texture2D red = SolidColorMaterials.NewSolidColorTexture(new Color32(160, 80, 90, 255));

        public static readonly Texture2D
            blue = SolidColorMaterials.NewSolidColorTexture(new Color32(80, 123, 160, 255));

        private static float groaner = 9999999;
        private static Vector2 scrolpos = Vector2.zero;
        private static Vector2 scrolpostabs = Vector2.zero;
        public static bool ShowSettings = true;
        public static bool PatchedEverything;

        public override void PreOpen()
        {
            base.PreOpen();
            if (!PatchedEverything)
            {
                Log.Message("Applying profiling patches...");
                try
                {
                    var modes = GenTypes.AllTypes.Where(m => m.TryGetAttribute<ProfileMode>(out _)).OrderBy(m => m.TryGetAttribute<ProfileMode>().name);

                    foreach (var mode in modes)
                    {

                        var att = mode.TryGetAttribute<ProfileMode>();
                        att.Settings = new Dictionary<FieldInfo, Setting>();

                        foreach (var fieldInfo in mode.GetFields().Where(m => m.TryGetAttribute<Setting>(out _)))
                        {
                            var sett = fieldInfo.TryGetAttribute<Setting>();
                            att.Settings.Add(fieldInfo, sett);
                        }

                        att.Clicked = AccessTools.Method(mode, "Clicked");
                        att.Selected = AccessTools.Method(mode, "Selected");
                        att.Checkbox = AccessTools.Method(mode, "Checkbox");
                        att.typeRef = mode;

                        foreach (var profileTab in MainTabs)
                        {
                            if (att.mode == profileTab.UpdateMode)
                            {
                                profileTab.Modes.Add(att, mode);
                            }
                        }
                    }

                    Analyzer.harmony.PatchAll(Assembly.GetExecutingAssembly());

                    PatchedEverything = true;
                    Log.Message("Done");
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }
            }

            Analyzer.StartProfiling();
        }

        public override void PostClose()
        {
            base.PostClose();
            Analyzer.StopProfiling();
            Analyzer.Reset();
            Analyzer.Settings.Write();
        }



        public static List<ProfileTab> MainTabs = new List<ProfileTab>
        {
            new ProfileTab("Home", () => ShowSettings = true, () => ShowSettings, UpdateMode.Dead, "Settings and utils"),
            new ProfileTab("Tick", () => { }, () => false, UpdateMode.Tick, "Things that run on tick"),
            new ProfileTab("Update", () => { }, () => false, UpdateMode.Update, "Things that run per frame"),
            new ProfileTab("GUI", () => { }, () => false, UpdateMode.GUI, "Things that run on GUI")
        };



        public static Listing_Standard listing = new Listing_Standard();


        //public static FloatMenu FM = new FloatMenu(new List<FloatMenuOption>
        //{
        //    new FloatMenuOption("First", () => Analyzer.SortBy = "First"),
        //    new FloatMenuOption("Usage", () => Analyzer.SortBy = "Usage"),
        //    new FloatMenuOption("A-Z", () => Analyzer.SortBy = "A-Z")
        //});

        public static string stlank = string.Empty;
        public static string stVector = string.Empty;
        public static long totalBytesOfMemoryUsed;


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

        public override Vector2 InitialSize => new Vector2(750, 650);

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

        private float moaner = 0;

        public override void DoWindowContents(Rect canvas)
        {
            if (Event.current.type == EventType.Layout)
            {
                return;
            }

            //  canvas.y += 35;
            //   canvas.height -= 35f;

            // Widgets.DrawMenuSection(canvas);

            // TabDrawer.DrawTabs(canvas, MainTabs, 150f);

            var ListerBox = canvas.LeftPart(0.30f);
            ListerBox.width -= 10f;
            Widgets.DrawMenuSection(ListerBox);
            ListerBox = ListerBox.ContractedBy(4f);
            var innyrek = ListerBox.AtZero();
            innyrek.width -= 16f;
            innyrek.height = moaner;
            var goat = 0f;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Tiny;
            Widgets.BeginScrollView(ListerBox, ref scrolpostabs, innyrek);
            GUI.BeginGroup(innyrek);
            listing.Begin(innyrek);
            // var row = listing.getr

            foreach (var maintab in MainTabs)
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                goat += 40f;
                var row = listing.GetRect(30f);
                if (maintab.Selected) Widgets.DrawOptionSelected(row);
                if (Widgets.ButtonInvisible(row)) maintab.clickedAction();
                row.x += 5f;
                Widgets.Label(row, maintab.label);

                TooltipHandler.TipRegion(row, maintab.Tip);

                Text.Anchor = TextAnchor.MiddleLeft;
                Text.Font = GameFont.Tiny;

                foreach (var mode in maintab.Modes)
                {
                    if (!mode.Key.Basics && !Analyzer.Settings.AdvancedMode)
                    {
                        continue;
                    }
                    row = listing.GetRect(30f);
                    Widgets.DrawHighlightIfMouseover(row);
                    if (Analyzer.SelectedMode == mode.Key) Widgets.DrawOptionSelected(row);
                    row.x += 20f;
                    goat += 30f;
                    Widgets.Label(row, mode.Key.name);
                    if (Widgets.ButtonInvisible(row))
                    {
                        if (ShowSettings)
                        {
                            ShowSettings = false;
                            Analyzer.Settings.Write();
                        }
                        if (Analyzer.SelectedMode != null)
                        {
                            AccessTools.Field(Analyzer.SelectedMode.typeRef, "Active").SetValue(null, false);
                        }
                        AccessTools.Field(mode.Value, "Active").SetValue(null, true);
                        Analyzer.SelectedMode = mode.Key;
                        Analyzer.Reset();

                        if (!mode.Key.IsPatched)
                        {
                            mode.Key.ProfilePatch();
                        }
                    }
                    TooltipHandler.TipRegion(row, mode.Key.tip);

                    if (Analyzer.SelectedMode == mode.Key)
                    {
                        var doo = 0;
                        foreach (var keySetting in mode.Key.Settings)
                        {
                            if (keySetting.Key.FieldType == typeof(bool))
                            {
                                row = listing.GetRect(30f);
                                row.x += 20f;
                                  GUI.color = Widgets.OptionSelectedBGBorderColor;
                                Widgets.DrawLineVertical(row.x, row.y, 15f);
                                if (doo != 0)
                                {
                                    Widgets.DrawLineVertical(row.x, row.y - 15f, 15f);
                                }
                                row.x += 10f;
                                Widgets.DrawLineHorizontal(row.x - 10f, row.y + 15f, 10f);
                                GUI.color = Color.white;
                                goat += 30f;
                                bool cur = (bool)keySetting.Key.GetValue(null);
                                if (DubGUI.Checkbox(row, keySetting.Value.name, ref cur))
                                {
                                    keySetting.Key.SetValue(null, cur);
                                    Analyzer.Reset();
                                }
                            }
                            if (keySetting.Key.FieldType == typeof(float) || keySetting.Key.FieldType == typeof(int))
                            {

                            }

                            doo++;
                        }
                    }
                }
            }

            listing.End();
            moaner = goat;
            GUI.EndGroup();

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.EndScrollView();


            var inner = canvas.RightPart(0.70f).Rounded();

            if (ShowSettings)
            {
                Analyzer.Settings.DoSettings(inner);
            }
            else
            {
                try
                {

                    if (PatchedEverything)
                    {
                        if (Dialog_Graph.key != string.Empty)
                        {
                            Rect blurg = inner.TopPart(0.75f).Rounded();
                            Widgets.DrawMenuSection(blurg);
                            blurg = blurg.ContractedBy(6f);
                            DoThingTab(blurg);
                            blurg = inner.BottomPart(0.25f).Rounded();
                            //blurg = blurg.BottomPartPixels(inner.height - 10f);
                            Dialog_Graph.DoGraph(blurg);
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
                        Text.Font = GameFont.Small;
                        Text.Anchor = TextAnchor.UpperLeft;
                    }

                }
                catch (Exception e)
                {
                    // Console.WriteLine(e);
                    //  throw;
                }
            }
        }

        public static string TimesFilter = string.Empty;
        public static StringBuilder csv = new StringBuilder();
        public Rect GizmoListRect;
        private void DoThingTab(Rect rect)
        {

            if (!Analyzer.SelectedMode.IsPatched)
            {
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, $"Loading{GenText.MarchingEllipsis(0f)}");
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            var topslot = rect.TopPartPixels(20f);
            Rect rowby = topslot.LeftPartPixels(25f);
            if (Widgets.ButtonImage(rowby, TexButton.SpeedButtonTextures[Analyzer.running ? 0 : 1]))
            {
                Analyzer.running = !Analyzer.running;
            }
            
            TooltipHandler.TipRegion(rowby, "Start and stop logging");
            bool save = false;
            if (Analyzer.Settings.AdvancedMode)
            {
                Rect searchbox = topslot.LeftPartPixels(topslot.width - 300f);
                searchbox.x += 25f;
                DubGUI.InputField(searchbox, "Search", ref TimesFilter, DubGUI.MintSearch);
                rowby.x = searchbox.xMax;
                rowby.width = 175f;
                if (Widgets.ButtonTextSubtle(rowby, stlank, Mathf.Clamp01(Mathf.InverseLerp(H_RootUpdate.LastMinGC, H_RootUpdate.LastMaxGC, totalBytesOfMemoryUsed)), 5))
                {
                    totalBytesOfMemoryUsed = GC.GetTotalMemory(true);
                }
                TooltipHandler.TipRegion(rowby, "Approximation of total bytes currently allocated in managed memory + rate of new allocation\n\nClick to force GC");

                rowby.x = rowby.xMax;
                rowby.width = 100f;
                save = Widgets.ButtonTextSubtle(rowby, "Save .CSV");
             TooltipHandler.TipRegion(rowby, $"Save the current list of times to a csv file in {GenFilePaths.FolderUnderSaveData("Profiling")}");
            }
            else
            {
                Rect searchbox = topslot.RightPartPixels(topslot.width - 25f);
                DubGUI.InputField(searchbox, "Search", ref TimesFilter, DubGUI.MintSearch);
            }

            rowby.x = rowby.xMax;
            rowby.width = 25f;


            rect.y += 25f;
            rect.height -= 25f;

            var innyrek = rect.AtZero();
            innyrek.width -= 16f;
            innyrek.height = groaner;

            GizmoListRect = rect.AtZero();
            GizmoListRect.y += scrolpos.y;
            Widgets.BeginScrollView(rect, ref scrolpos, innyrek);

            GUI.BeginGroup(innyrek);

            listing.Begin(innyrek);

            float goat = 0;

            //List<ProfileLog> logs = null;

            //if (Analyzer.SortBy == "First")
            //{
            //    logs = Analyzer.Logs.ToList();
            //}
            //else if (Analyzer.SortBy == "A-Z")
            //{
            //    logs = Analyzer.Logs.ToList().OrderBy(x => x.Key).ToList();
            //}
            //else if (Analyzer.SortBy == "Usage")
            //{
            //    logs = Analyzer.Logs.ToList().OrderByDescending(x => x.Average).ToList();
            //}

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Tiny;

            lock (Analyzer.sync)
            {
                foreach (var log in Analyzer.Logs)
                {
                    if (!log.Label.Has(TimesFilter))
                    {
                        continue;
                    }

                    var r = listing.GetRect(40f);

                    if (r.Overlaps(GizmoListRect))
                    {
                        var profile = Analyzer.Profiles[log.Key];

                        if (Input.GetKey(KeyCode.LeftControl))
                        {
                            if (Widgets.ButtonInvisible(r))
                            {
                                Analyzer.SelectedMode.Clicked?.Invoke(null, new object[] { profile, log });
                                Analyzer.Settings.Write();
                            }
                        }

                        bool on = true;

                        if (Analyzer.SelectedMode.Selected != null)
                        {
                            on = (bool)Analyzer.SelectedMode.Selected.Invoke(null, new object[] { profile, log });
                        }

                        if (Analyzer.SelectedMode.Checkbox != null)
                        {
                            var r2 = new Rect(r.x, r.y, 25f, r.height);
                            r.x += 25f;
                            if (DubGUI.Checkbox(r2, "", ref on))
                            {
                                Analyzer.SelectedMode.Checkbox?.Invoke(null, new object[] { profile, log });
                                Analyzer.Settings.Write();
                            }
                        }

                        Widgets.DrawHighlightIfMouseover(r);

                        if (Widgets.ButtonInvisible(r))
                        {
                            Dialog_Graph.RunKey(log.Key);
                        }

                        if (Dialog_Graph.key == log.Key)
                        {
                            Widgets.DrawHighlightSelected(r);
                        }


                        var col = grey;
                        if (log.Percent > 0.25f)
                        {
                            col = blue;
                        }

                        if (log.Percent > 0.75f)
                        {
                            col = red;
                        }


                        Widgets.FillableBar(r.BottomPartPixels(8f), log.Percent, col, clear, false);

                        r = r.LeftPartPixels(50);

                        if (!on)
                        {
                            GUI.color = Color.grey;
                        }

                        Widgets.Label(r, log.Average_s);

                        if (Analyzer.Settings.AdvancedMode)
                        {
                            r.x = r.xMax;

                            Widgets.Label(r, profile.memRiseStr);
                        }

                        r.x = r.xMax;
                        r.width = 2000;
                        Widgets.Label(r, log.Label);

                        GUI.color = Color.white;

                        if (save)
                        {
                            csv.Append($"{log.Label},{log.Average},{profile.BytesUsed}");
                            foreach (var historyTime in profile.History.times)
                            {
                                csv.Append($",{historyTime}");
                            }
                            csv.AppendLine();
                        }
                    }
                    listing.GapLine(0f);
                    goat += 4f;
                    goat += r.height;
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
            groaner = goat;
            GUI.EndGroup();

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.EndScrollView();
        }
    }
}