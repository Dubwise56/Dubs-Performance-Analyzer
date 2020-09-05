//using ColourPicker;
//using HarmonyLib;
//using RimWorld;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Threading;
//using UnityEngine;
//using Verse;

///*  Naming Wise
// *  Tabs on the side, Ex 'HarmonyPatches', SideTab
// *  Categories for them, Ex 'Tick', SideTabCategories
// *  A Log 'inside' a SideTab, is a 'Log', each Log belongs to a SideTab
// */

//namespace DubsAnalyzer
//{

//    [StaticConstructorOnStartup]
//    public class Dialog_Analyzer : Window
//    {
//        public override Vector2 InitialSize => new Vector2(890, 650);

//        private readonly SideTab sideTab;
//        private readonly Logs logs;
//        private readonly AdditionalInfo additionalInfo;

//        public static string GarbageCollectionInfo = string.Empty;

//        public static string TimesFilter = string.Empty;
//        public static StringBuilder csv = new StringBuilder();

//        public static long totalBytesOfMemoryUsed;

//        private static Thread CleanupPatches = null;
//        public static List<Action> QueuedMessages = new List<Action>();
//        public static object messageSync = new object();

//        public static float cache = -1;
//        public static float cachedWidth = -1;
//        public static bool OnFirstLoad = true;
//        public override void PreOpen()
//        {
//            base.PreOpen();
//            Reboot();

//            if (cachedWidth != -1)
//                windowRect.width = cachedWidth;
//        }


//        public static void Reboot()
//        {
//            if (AnalyzerState.CanPatch())
//            {
//                AnalyzerState.State = CurrentState.Patching;
//                Log.Message("Applying profiling patches...");
//                try
//                {
//                    if (OnFirstLoad)
//                    {
//                        LoadModes();
//                        OnFirstLoad = false;
//                    }


//                    Modbase.harmony.PatchAll(Assembly.GetExecutingAssembly());

//                    Log.Message("Done");
//                }
//                catch (Exception e)
//                {
//                    Log.Error(e.ToString());
//                }
//            }

//            AnalyzerState.State = CurrentState.Open;
//            Modbase.StartProfiling();
//        }

//        public static void LoadModes()
//        {
//            List<Type> modes = GenTypes.AllTypes.Where(m => m.TryGetAttribute<ProfileMode>(out _)).OrderBy(m => m.TryGetAttribute<ProfileMode>().name).ToList();

//            foreach (Type mode in modes)
//            {
//                try
//                {
//                    ProfileMode att = mode.TryGetAttribute<ProfileMode>();
//                    att.Settings = new Dictionary<FieldInfo, Setting>();

//                    foreach (FieldInfo fieldInfo in mode.GetFields().Where(m => m.TryGetAttribute<Setting>(out _)))
//                    {
//                        Setting sett = fieldInfo.TryGetAttribute<Setting>();
//                        att.Settings.SetOrAdd(fieldInfo, sett);
//                    }
//                    att.MouseOver = AccessTools.Method(mode, "MouseOver");
//                    att.Clicked = AccessTools.Method(mode, "Clicked");
//                    att.Selected = AccessTools.Method(mode, "Selected");
//                    att.Checkbox = AccessTools.Method(mode, "Checkbox");
//                    att.typeRef = mode;

//                    foreach (ProfileTab profileTab in AnalyzerState.SideTabCategories)
//                    {
//                        if (att.mode == profileTab.UpdateMode)
//                            profileTab.Modes.SetOrAdd(att, mode);
//                    }
//                }
//                catch (Exception e)
//                {
//                    Log.Error(e.ToString());
//                }
//            }

//            foreach (ProfileMode profileMode in ProfileMode.instances)
//            {
//                foreach (ProfileTab profileTab in AnalyzerState.SideTabCategories)
//                {
//                    if (profileMode.mode == profileTab.UpdateMode)
//                    {
//                        if (profileTab.Modes.Keys.All(x => x.name != profileMode.name))
//                        {
//                            profileTab.Modes.Add(profileMode, null);
//                        }
//                    }
//                }
//            }
//        }

//        public override void PostClose()
//        {
//            base.PostClose();
//            Modbase.StopProfiling();
//            Modbase.Reset();
//            Modbase.Settings.Write();

//            foreach (ProfileTab tab in AnalyzerState.SideTabCategories)
//            {
//                foreach (KeyValuePair<ProfileMode, Type> mode in tab.Modes)
//                {
//                    mode.Key.SetActive(false);
//                }
//            }

//            // we add new functionality
//            if (AnalyzerState.CanCleanup())
//            {
//                CleanupPatches = new Thread(() => Modbase.UnPatchMethods());
//                CleanupPatches.Start();
//            }


//        }

//        public Dialog_Analyzer()
//        {
//            layer = WindowLayer.Super;
//            forcePause = false;
//            absorbInputAroundWindow = false;
//            closeOnCancel = false;
//            soundAppear = SoundDefOf.CommsWindow_Open;
//            soundClose = SoundDefOf.CommsWindow_Close;
//            doCloseButton = false;
//            doCloseX = true;
//            draggable = true;
//            drawShadow = true;
//            preventCameraMotion = false;
//            onlyOneOfTypeAllowed = true;
//            resizeable = true;

//            sideTab = new SideTab(this);
//            logs = new Logs(this);
//            additionalInfo = new AdditionalInfo(this);
//        }

//        public override void SetInitialSizeAndPosition()
//        {
//            windowRect = new Rect(50f, (UI.screenHeight - InitialSize.y) / 2f, InitialSize.x, InitialSize.y);
//            windowRect = windowRect.Rounded();
//        }

//        public override void DoWindowContents(Rect canvas)
//        {
//            if (Event.current.type == EventType.Layout) return;

//            try
//            {
//                cachedWidth = windowRect.width;

//                /*
//                 * Draw our side tab, including our:
//                 * - Categories (Home, Modding Tools, Tick, Update, GUI)
//                 * - Content (Modes inside each of the above categories)
//                 */
//                sideTab.Draw(canvas);

//                /*
//                 * Draw the actual screen we want, either:
//                 * - Home Screen, Modders Tools, or one of the categories
//                 */
//                Rect inner = canvas;
//                inner.x += SideTab.width;
//                inner.width -= SideTab.width;

//                switch (AnalyzerState.CurrentSideTabCategory)
//                {
//                    case SideTabCategory.Home:
//                        if (cache != -1)
//                        {
//                            windowRect.width -= 450;
//                            cache = -1;
//                        }
//                        Modbase.Settings.DoSettings(inner);
//                        break;
//                    default: // We are in one of our categories, which means we want to display our logs
//                        /*
//                         * Draw our top row, we will always show this, unconditionally
//                         */
//                        bool save = false;

//                        if (!(AnalyzerState.State == CurrentState.Open) || AnalyzerState.CurrentProfileKey == "") // if we aren't currently 'open' draw a loading sign, and leave
//                        {
//                            DrawLoading(inner);
//                            return;
//                        }

//                        if (Modbase.Settings.SidePanel)
//                        {
//                            if (cache == -1)
//                            {
//                                windowRect.width += 450;
//                                cache = windowRect.width;
//                            }

//                            inner = canvas.LeftPartPixels(Logs.width);
//                            inner.x += SideTab.width;

//                            DrawTopRow(inner.TopPartPixels(20f), ref save);
//                            inner.y += 25;
//                            inner.height -= 25;
//                            Widgets.DrawMenuSection(inner);
//                            logs.Draw(inner, save);

//                            Rect AdditionalInfoBox = canvas.RightPartPixels(canvas.width - (Logs.width + SideTab.width - 1f)).Rounded();
//                            if (AnalyzerState.CurrentProfileKey == "Overview")
//                            {
//                                Dialog_StackedGraph.Display(AdditionalInfoBox);
//                            }
//                            else
//                            {
//                                additionalInfo.DrawPanel(AdditionalInfoBox);
//                            }
//                        }
//                        else
//                        {
//                            if (cache != -1)
//                            {
//                                windowRect.width -= 450;
//                                cache = -1;
//                            }

//                            DrawTopRow(inner.TopPartPixels(20f), ref save);
//                            inner.y += 25;
//                            inner.height -= 25;

//                            Rect AdditionalInfoBox = canvas.BottomPart(.5f);
//                            if (AnalyzerState.CurrentProfileKey == "Overview")
//                            {
//                                Widgets.DrawMenuSection(inner);
//                                logs.Draw(inner, save);
//                                Dialog_StackedGraph.Display(AdditionalInfoBox);
//                            }
//                            else
//                            {
//                                inner = inner.TopPart(0.5f);
//                                Widgets.DrawMenuSection(inner);
//                                logs.Draw(inner, save);
//                                additionalInfo.Draw(AdditionalInfoBox);
//                            }
//                        }

//                        break;
//                }

//                // Now we are outside the scope of all of our gui, lets print the messages we had queued during this time
//                lock (messageSync)
//                {
//                    foreach (Action action in QueuedMessages)
//                        action();

//                    QueuedMessages.Clear();
//                }
//            }
//            catch (Exception e)
//            {
//                Log.Error($"[Analyzer] Caught the error {e.Message}, handling and closing scroll and gui scope");

//                GUI.EndScrollView();
//                GUI.EndGroup();
//            }

//        }


//        private void DrawLoading(Rect rect)
//        {
//            Widgets.DrawMenuSection(rect);
//            Text.Font = GameFont.Medium;
//            Text.Anchor = TextAnchor.MiddleCenter;
//            Widgets.Label(rect, $"Loading{GenText.MarchingEllipsis(0f)}");
//            DubGUI.ResetFont();
//        }

//        private void DrawTopRow(Rect topRow, ref bool save)
//        {
//            Rect row = topRow.LeftPartPixels(25f);

//            if (Widgets.ButtonImage(row, TexButton.SpeedButtonTextures[AnalyzerState.CurrentlyRunning ? 0 : 1]))
//            {
//                AnalyzerState.CurrentlyRunning = !AnalyzerState.CurrentlyRunning;
//                AnalyzerState.CurrentTab.SetActive(AnalyzerState.CurrentlyRunning);
//            }

//            TooltipHandler.TipRegion(topRow, "startstoplogTip".Translate());
//            save = false;

//            Rect searchbox = topRow.LeftPartPixels(topRow.width - 350f);
//            searchbox.x += 25f;
//            DubGUI.InputField(searchbox, "Search", ref TimesFilter, DubGUI.MintSearch);
//            row.x = searchbox.xMax + 5;
//            row.width = 130f;
//            Text.Anchor = TextAnchor.MiddleCenter;
//            Text.Font = GameFont.Tiny;
//            Widgets.FillableBar(row, Mathf.Clamp01(Mathf.InverseLerp(H_RootUpdate.LastMinGC, H_RootUpdate.LastMaxGC, totalBytesOfMemoryUsed)), DubResources.darkgrey);
//            Widgets.Label(row, GarbageCollectionInfo);
//            Text.Anchor = TextAnchor.UpperLeft;
//            TooltipHandler.TipRegion(row, "garbageTip".Translate());

//            row.x = row.xMax + 5;
//            row.width = 50f;
//            Widgets.Label(row, H_RootUpdate._fpsText);
//            TooltipHandler.TipRegion(row, "fpsTipperino".Translate());
//            row.x = row.xMax + 5;
//            row.width = 90f;
//            Widgets.Label(row, H_RootUpdate.tps);
//            TooltipHandler.TipRegion(row, "tpsTipperino".Translate());
//            row.x = row.xMax + 5;
//            row.width = 30f;
//            Text.Font = GameFont.Medium;
//            save = Widgets.ButtonImageFitted(row, DubResources.sav);
//            TooltipHandler.TipRegion(row, "savecsvTip".Translate(GenFilePaths.FolderUnderSaveData("Profiling")));

//            row.x = row.xMax;
//            row.width = 25f;
//        }


//        internal class SideTab
//        {
//            private readonly Dialog_Analyzer super = null;
//            public static float width = 220f;
//            private static Vector2 ScrollPosition = Vector2.zero;
//            public static Listing_Standard listing = new Listing_Standard();
//            public static float yOffset = 0f;
//            private float ListHeight = 0;

//            public SideTab(Dialog_Analyzer super)
//            {
//                this.super = super;
//            }

//            public void Draw(Rect rect)
//            {
//                Rect ListerBox = rect.LeftPartPixels(width);
//                ListerBox.width -= 10f;
//                Widgets.DrawMenuSection(ListerBox);
//                ListerBox = ListerBox.ContractedBy(4f);

//                Rect baseRect = ListerBox.AtZero();
//                baseRect.width -= 16f;
//                baseRect.height = ListHeight;

//                Text.Anchor = TextAnchor.MiddleLeft;
//                Text.Font = GameFont.Tiny;

//                yOffset = 0f;

//                { // Begin Scope for Scroll & GUI Group/View
//                    Widgets.BeginScrollView(ListerBox, ref ScrollPosition, baseRect);
//                    GUI.BeginGroup(baseRect);
//                    listing.Begin(baseRect);

//                    foreach (ProfileTab maintab in AnalyzerState.SideTabCategories)
//                        if (!(maintab.label == "Modder Added" && maintab.Modes.Count == 0))
//                            DrawSideTabList(maintab);

//                    listing.End();
//                    GUI.EndGroup();
//                    Widgets.EndScrollView();
//                }


//                DubGUI.ResetFont();
//                ListHeight = yOffset;
//            }

//            private void DrawSideTabList(Tab tab)
//            {
//                DubGUI.ResetFont();
//                yOffset += 40f;

//                Rect row = listing.GetRect(30f);

//                if (tab.Selected) Widgets.DrawOptionSelected(row);

//                if (tab.label == "Home" || tab.label == "Modder Tools")
//                {
//                    if (Widgets.ButtonInvisible(row))
//                        tab.clickedAction();
//                }
//                else
//                {
//                    if (Widgets.ButtonInvisible(row.LeftPartPixels(row.width - row.height)))
//                        tab.clickedAction();


//                    if (Widgets.ButtonImage(row.RightPartPixels(row.height), tab.Collapsed ? DubGUI.DropDown : DubGUI.FoldUp))
//                        tab.Collapsed = !tab.Collapsed;
//                }
//                row.x += 5f;
//                Widgets.Label(row, tab.label);

//                TooltipHandler.TipRegion(row, tab.Tip);

//                Text.Anchor = TextAnchor.MiddleLeft;
//                Text.Font = GameFont.Tiny;

//                if (tab.collapsed) return;

//                foreach (KeyValuePair<Entry, Type> mode in tab.entries)
//                {
//                    DrawSideTab(ref row, mode, tab.updateMode);
//                }
//            }

//            private void DrawSideTab(ref Rect row, KeyValuePair<Entry, Type> mode, UpdateMode updateMode)
//            {
//                row = listing.GetRect(30f);
//                Widgets.DrawHighlightIfMouseover(row);

//                if (AnalyzerState.CurrentTab == mode.Key)
//                    Widgets.DrawOptionSelected(row);

//                row.x += 20f;
//                yOffset += 30f;

//                Widgets.Label(row, mode.Key.name);

//                if (Widgets.ButtonInvisible(row))
//                {
//                    AnalyzerState.SwapTab(mode, updateMode);
//                }
//                if (mode.Key.Closable)
//                {
//                    if (Input.GetMouseButtonDown(1)) // mouse button right
//                    {
//                        if (row.Contains(Event.current.mousePosition))
//                        {
//                            List<FloatMenuOption> options = new List<FloatMenuOption>()
//                            {
//                                new FloatMenuOption("Close", () => AnalyzerState.RemoveTab(mode))
//                            };
//                            Find.WindowStack.Add(new FloatMenu(options));
//                        }
//                    }
//                }

//                TooltipHandler.TipRegion(row, mode.Key.tip);

//                if (AnalyzerState.CurrentTab == mode.Key)
//                {
//                    bool firstEntry = true;
//                    foreach (KeyValuePair<FieldInfo, Setting> keySetting in mode.Key.Settings)
//                    {
//                        if (keySetting.Key.FieldType == typeof(bool))
//                        {
//                            row = listing.GetRect(30f);
//                            row.x += 20f;
//                            GUI.color = Widgets.OptionSelectedBGBorderColor;
//                            Widgets.DrawLineVertical(row.x, row.y, 15f);

//                            if (!firstEntry)
//                            {
//                                Widgets.DrawLineVertical(row.x, row.y - 15f, 15f);
//                            }

//                            row.x += 10f;
//                            Widgets.DrawLineHorizontal(row.x - 10f, row.y + 15f, 10f);
//                            GUI.color = Color.white;
//                            yOffset += 30f;

//                            bool cur = (bool)keySetting.Key.GetValue(null);

//                            if (DubGUI.Checkbox(row, keySetting.Value.name, ref cur))
//                            {
//                                keySetting.Key.SetValue(null, cur);
//                                Modbase.Reset();
//                            }
//                        }

//                        if (keySetting.Value.tip != null)
//                        {
//                            TooltipHandler.TipRegion(row, keySetting.Value.tip);
//                        }

//                        firstEntry = false;
//                    }
//                }
//            }

//        }
            
//        internal class AdditionalInfo
//        {
//            public Dialog_Analyzer super = null;
//            public Graph graph = null;
//            public Listing_Standard listing = null;
//            public static Vector2 ScrollPosition = Vector2.zero;
//            public GameFont font = GameFont.Tiny;
//            public float CurX = 0;
//            public AdditionalInfo(Dialog_Analyzer super)
//            {
//                this.super = super;
//                this.graph = new Graph(this);
//            }

//            /*
//             * Bottom Panel
//             */
//            public void Draw(Rect rect)
//            {
//                GameFont oldFont = Text.Font;
//                listing = new Listing_Standard();

//                Rect ListerBox = rect.TopPart(.4f);
//                Widgets.DrawMenuSection(rect);
//                ListerBox = ListerBox.AtZero();

//                { // Begin Scope for Scroll & GUI Group/View
//                    Widgets.BeginScrollView(rect, ref ScrollPosition, ListerBox);
//                    GUI.BeginGroup(ListerBox);
//                    listing.Begin(ListerBox);
//                    Text.Font = font;

//                    DrawGeneral(ListerBox);

//                    { // graph settings
//                        Rect graphRect = ListerBox.BottomPartPixels(30f).RightPartPixels(325);
//                        graph.DrawSettings(graphRect.ContractedBy(2f), Graph.Entries);
//                    }

//                    listing.End();
//                    GUI.EndGroup();
//                    Widgets.EndScrollView();
//                }
//                Rect GraphBox = rect.BottomPart(.6f);
//                graph.Draw(GraphBox);

//                Text.Font = oldFont;
//                CurX = 0;
//            }
//            private float GetLongestStat()
//            {
//                string[] s1 = new string[]{
//                $" Entries: {CurrentLogStats.stats.Entries}", $" Highest Time: {CurrentLogStats.stats.HighestTime:0.000}ms",
//                $" Highest Calls (per frame): {CurrentLogStats.stats.HighestCalls}", $" μ calls (per frame): {CurrentLogStats.stats.MeanCallsPerFrame:0.00}",
//                $" μ time (per call): {CurrentLogStats.stats.MeanTimePerCall:0.000}ms", $" μ time (per frame): {CurrentLogStats.stats.MeanTimePerFrame:0.000}ms",
//                $" σ {CurrentLogStats.stats.OutlierCutoff:0.000}ms", $" Number of Spikes: {CurrentLogStats.stats.Spikes.Count}", $" % in category {AnalyzerState.CurrentLog.Average_s}"};

//                Vector2 vec = Vector2.zero;
//                foreach (string str in s1)
//                {
//                    if (Text.CalcSize(str).x > vec.x)
//                        vec = Text.CalcSize(str);
//                }

//                return vec.x;
//            }
//            private void DrawGeneral(Rect rect)
//            {
//                if (!LogStats.IsActiveThread)
//                {
//                    LogStats s = new LogStats();
//                    s.GenerateStats();
//                }

//                if (CurrentLogStats.stats == null)
//                {
//                    Text.Font = GameFont.Small;
//                    Text.Anchor = TextAnchor.MiddleCenter;
//                    listing.Label($"Loading{GenText.MarchingEllipsis(0f)}");
//                    DubGUI.ResetFont();
//                }
//                else
//                {
//                    lock (CurrentLogStats.sync)
//                    {
//                        //DrawStatsPageSidePanel();
//                        float longestStat = GetLongestStat();
//                        Rect statRect = rect.LeftPartPixels(longestStat);
//                        Widgets.Label(statRect,
//                        $" Entries: {CurrentLogStats.stats.Entries}\n Highest Time: {CurrentLogStats.stats.HighestTime:0.000}\n" +
//                        $" Highest Calls (per frame): {CurrentLogStats.stats.HighestCalls}\n μ calls (per frame): {CurrentLogStats.stats.MeanCallsPerFrame:0.00}\n" +
//                        $" % in category {AnalyzerState.CurrentLog.Average_s}\n" + $" μ time (per call): {CurrentLogStats.stats.MeanTimePerCall:0.000}ms\n" +
//                        $"μ time (per frame): {CurrentLogStats.stats.MeanTimePerFrame:0.000}ms\n σ {CurrentLogStats.stats.OutlierCutoff:0.000}ms\n Number of Spikes: {CurrentLogStats.stats.Spikes.Count}");
//                        CurX = longestStat;
//                    }
//                }
//            }

//            /*
//             * Side Panel
//             */
//            public void DrawPanel(Rect rect)
//            {
//                listing = new Listing_Standard();

//                Rect ListerBox = rect.TopPart(.75f);
//                ListerBox.width -= 10f;
//                Widgets.DrawMenuSection(ListerBox);
//                ListerBox = ListerBox.AtZero();

//                { // Begin Scope for Scroll & GUI Group/View
//                    Widgets.BeginScrollView(rect, ref ScrollPosition, ListerBox);
//                    GUI.BeginGroup(ListerBox);
//                    listing.Begin(ListerBox);

//                    DrawGeneralSidePanel();
//                    DrawStatisticsSidePanel();
//                    if (Modbase.Settings.AdvancedMode)
//                    {
//                        DrawStackTraceSidePanel();
//                        //DrawHarmonyOptionsSidePanel();
//                    }

//                    listing.End();
//                    GUI.EndGroup();
//                    Widgets.EndScrollView();
//                }
//                Rect GraphBox = rect.BottomPart(.25f);
//                GraphBox.width -= 10f;
//                graph.Draw(GraphBox);
//            }

//            private void DrawGeneralSidePanel()
//            {
//                DubGUI.Heading(listing, "General");
//                Text.Font = font;

//                if (Analyzer.GetProfiler() != null)
//                {
//                    string ass = AnalyzerState.CurrentProfiler().meth.DeclaringType.Assembly.FullName;
//                    string assname = "";
//                    if (ass.Contains("Assembly-CSharp")) assname = "Rimworld - Core";
//                    else if (ass.Contains("UnityEngine")) assname = "Rimworld - Unity";
//                    else if (ass.Contains("System")) assname = "Rimworld - System";
//                    else
//                    {
//                        try
//                        {
//                            assname = ModInfoCache.AssemblyToModname[ass];
//                        }
//                        catch (Exception) { assname = "Failed to locate assembly information"; }
//                    }

//                    DubGUI.InlineDoubleMessage($" Mod: {assname}", $" Assembly: {ass.Split(',').First()}.dll", listing, true);
//                    TextAnchor anch = Text.Anchor;
//                    Text.Anchor = TextAnchor.MiddleCenter;
//                    string str = $"{AnalyzerState.CurrentProfiler().meth.DeclaringType.FullName}:{AnalyzerState.CurrentProfiler().meth.Name}";
//                    float strLen = Text.CalcHeight(str, listing.ColumnWidth * .95f);

//                    Rect rect = listing.GetRect(strLen);

//                    Widgets.Label(rect, str);

//                    if (Modbase.Settings.AdvancedMode)
//                    {
//                        Widgets.DrawHighlightIfMouseover(rect);
//                        if (Input.GetMouseButtonDown(1) && rect.Contains(Event.current.mousePosition)) // mouse button right
//                        {
//                            List<FloatMenuOption> options = new List<FloatMenuOption>()
//                            {
//                                new FloatMenuOption("Open In Github", () => OpenGithub()),
//                            };
//                            if (Modbase.Settings.DevMode)
//                            {
//                                options.Add(new FloatMenuOption("Open In Dnspy (requires local path)", () => OpenDnspy()));
//                            }
//                            Find.WindowStack.Add(new FloatMenu(options));
//                        }
//                    }

//                    void OpenGithub()
//                    {
//                        Application.OpenURL(@"https://github.com/search?l=C%23&q=" + $"{AnalyzerState.CurrentProfiler().meth.DeclaringType}.{AnalyzerState.CurrentProfiler().meth.Name}" + "&type=Code");
//                    }

//                    void OpenDnspy()
//                    {
//                        if (Modbase.Settings.PathToDnspy == "" || Modbase.Settings.PathToDnspy == null)
//                        {
//                            Log.ErrorOnce("You have not given a local path to dnspy", 10293838);
//                            return;
//                        }

//                        MethodBase meth = AnalyzerState.CurrentProfiler().meth;
//                        string path = meth.DeclaringType.Assembly.Location;
//                        if (path == null || path.Length == 0)
//                        {
//                            ModContentPack contentPack = LoadedModManager.RunningMods.FirstOrDefault(m => m.assemblies.loadedAssemblies.Contains(meth.DeclaringType.Assembly));
//                            if (contentPack != null)
//                            {
//                                path = ModContentPack.GetAllFilesForModPreserveOrder(contentPack, "Assemblies/", p => p.ToLower() == ".dll", null)
//                                    .Select(fileInfo => fileInfo.Item2.FullName)
//                                    .First(dll =>
//                                    {
//                                        Assembly assembly = Assembly.ReflectionOnlyLoadFrom(dll);
//                                        return assembly.GetType(meth.DeclaringType.FullName) != null;
//                                    });
//                            }
//                        }
//                        int token = meth.MetadataToken;
//                        if (token != 0)
//                            Process.Start(Modbase.Settings.PathToDnspy, $"\"{path}\" --select 0x{token:X8}");

//                    }
//                }
//                else
//                {
//                    listing.Label("Failed to grab the method associated with this entry - please report this");
//                }
//                listing.GapLine(0f);
//            }

//            private void DrawStatisticsSidePanel()
//            {

//                DubGUI.CollapsableHeading(listing, "Statistics", ref AnalyzerState.HideStatistics);
//                Text.Font = font;

//                if (!LogStats.IsActiveThread)
//                {
//                    LogStats s = new LogStats();
//                    s.GenerateStats();
//                }

//                if (CurrentLogStats.stats == null)
//                {
//                    Text.Font = GameFont.Small;
//                    Text.Anchor = TextAnchor.MiddleCenter;
//                    listing.Label($"Loading{GenText.MarchingEllipsis(0f)}");
//                    DubGUI.ResetFont();
//                }
//                else
//                {
//                    if (!AnalyzerState.HideStatistics)
//                    {
//                        lock (CurrentLogStats.sync)
//                        {
//                            //var headings = new string[] { " Total:", " Highest:" };
//                            //Text.Font = GameFont.Medium;
//                            //var highest = Mathf.Max(Text.CalcHeight(headings[0], listing.ColumnWidth / 2.0f), Text.CalcHeight(headings[1], listing.ColumnWidth / 2.0f));

//                            //var headingRect = listing.GetRect(highest);

//                            //var leftSide = headingRect.LeftPart(.48f);
//                            //var rightSide = headingRect.RightPart(.48f);

//                            //Widgets.Label(leftSide, headings[0]);
//                            //Widgets.Label(rightSide, headings[1]);

//                            //Text.Font = GameFont.Small;
//                            //DubGUI.InlineDoubleMessageNC($" Entries: {CurrentLogStats.stats.Entries}", $" Number of Spikes: {CurrentLogStats.stats.Spikes.Count} ", listing, false);
//                            //DubGUI.InlineDoubleMessageNC($" Calls: {CurrentLogStats.stats.TotalCalls}", $" Highest Calls (per frame): {CurrentLogStats.stats.HighestCalls}", listing, false);
//                            //DubGUI.InlineDoubleMessageNC($" Time: {CurrentLogStats.stats.TotalTime:0.000}ms ", $" Highest Time: {CurrentLogStats.stats.HighestTime:0.000}ms", listing, true);

//                            //var secondheadings = new string[] { " Per Frame ", " Per Call " };

//                            //Text.Font = GameFont.Medium;
//                            //listing.Label(secondheadings[0]);
//                            //Text.Font = GameFont.Small;

//                            //listing.Label($" μ calls (per frame): {CurrentLogStats.stats.MeanCallsPerFrame:0.00}");
//                            //listing.Label($" μ time (per frame): {CurrentLogStats.stats.MeanTimePerFrame:0.000}ms ");


//                            //Text.Font = GameFont.Medium;
//                            //listing.Label(secondheadings[1]);
//                            //Text.Font = GameFont.Small;

//                            //listing.Label($" μ time (per call): {CurrentLogStats.stats.MeanTimePerCall:0.000}ms ");


//                            DubGUI.InlineTripleMessage($" Entries: {CurrentLogStats.stats.Entries}", $" Σ Calls: {CurrentLogStats.stats.TotalCalls}", $" Σ Time: {CurrentLogStats.stats.TotalTime:0.000}ms ", listing, true);
//                            DubGUI.InlineTripleMessage($" Highest Time: {CurrentLogStats.stats.HighestTime:0.000}ms", $" Highest Calls (per frame): {CurrentLogStats.stats.HighestCalls}", $" μ time (per call): {CurrentLogStats.stats.MeanTimePerCall:0.000}ms ", listing, true);
//                            DubGUI.InlineTripleMessage($" % in category {AnalyzerState.CurrentLog.Average_s}", $" μ calls (per frame): {CurrentLogStats.stats.MeanCallsPerFrame:0.00}", $" μ time (per frame): {CurrentLogStats.stats.MeanTimePerFrame:0.000}ms ", listing, true);
//                            DubGUI.InlineDoubleMessage($" σ {CurrentLogStats.stats.OutlierCutoff:0.000}ms", $" Number of Spikes: {CurrentLogStats.stats.Spikes.Count} ", listing, true);
//                        }
//                    }
//                }
//            }

//            private void DrawStackTraceSidePanel()
//            {
//                if (Modbase.Settings.AdvancedMode)
//                {
//                    DubGUI.CollapsableHeading(listing, "Stack Trace", ref AnalyzerState.HideStacktrace);
//                    Text.Font = font;
//                }

//                if (!AnalyzerState.HideStacktrace)
//                {
//                    listing.Label($"Stacktraces: {StackTraceRegex.traces.Count}");

//                    foreach (KeyValuePair<string, StackTraceInformation> st in StackTraceRegex.traces.OrderBy(w => w.Value.Count).Reverse())
//                    {
//                        int i = 0;
//                        StackTraceInformation traceInfo = st.Value;

//                        for (i = 0; i < st.Value.TranslatedArr().Count() - 2; i++)
//                        {
//                            DrawTrace(i, false);
//                        }

//                        DrawTrace(i, true);

//                        void DrawTrace(int idx, bool capoff)
//                        {
//                            Rect rect = DubGUI.InlineDoubleMessage(
//                                traceInfo.TranslatedArr()[idx], traceInfo.methods[idx].Item2.Count.ToString(), listing,
//                                capoff).LeftPart(.5f);

//                            if (Mouse.IsOver(rect))
//                            {
//                                StringBuilder builder = new StringBuilder();
//                                foreach (StackTraceInformation.HarmonyPatch p in traceInfo.methods[idx].Item2)
//                                    GetString(p);

//                                void GetString(StackTraceInformation.HarmonyPatch patch)
//                                {
//                                    if (patch.id != Modbase.harmony.Id && patch.id != Modbase.perfharmony.Id &&
//                                        patch.id != InternalMethodUtility.Harmony.Id)
//                                    {
//                                        string ass = patch.patch.DeclaringType.Assembly.FullName;
//                                        string assname = ModInfoCache.AssemblyToModname[ass];

//                                        builder.AppendLine(
//                                            $"{patch.type} from {assname} with the index {patch.index} and the priority {patch.priority}\n");
//                                    }
//                                }

//                                TooltipHandler.TipRegion(rect, builder.ToString());
//                            }
//                        }
//                    }
//                }
//            }

//            //private void DrawHarmonyOptionsSidePanel()
//            //{
//            //    DubGUI.Heading(listing, "Information");
//            //}
//    }
//}


