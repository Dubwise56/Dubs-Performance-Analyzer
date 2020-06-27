using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DubsAnalyzer
{
    [StaticConstructorOnStartup]
    public class Gfx
    {
        public static Texture2D disco = ContentFinder<Texture2D>.Get("DPA/UI/discord", false);
        public static Texture2D Support = ContentFinder<Texture2D>.Get("DPA/UI/Support", false);
    }

    public class PerfAnalSettings : ModSettings
    {
        public static Listing_Standard listing = new Listing_Standard();

        public static string methToPatch = string.Empty;



        public static List<MethodInfo> GotMeth = new List<MethodInfo>();
        public bool UnlockFramerate = false;
        public Color LineCol = new Color32(79, 147, 191, 255);
        public Color GraphCol = new Color32(17, 17, 17, 255);
        public static string MethSearch = string.Empty;
        public static string TypeSearch = string.Empty;
        public Dictionary<Type, bool> AlertFilter = new Dictionary<Type, bool>();
        // public bool FixBedMemLeak;
        public bool FixGame;
        public bool HumanoidOnlyWarden;
        public bool KillMusicMan;
        public Dictionary<string, bool> Loggers = new Dictionary<string, bool>();
        public bool MeshOnlyBuildings;
        public bool NeverCheckJobsOnDamage;
        public bool FixRepair;
        public bool OptimizeDrawInspectGizmoGrid;
        public bool OverrideAlerts;
        public bool OptimizeAlerts;
        public bool OptimizeDrills;
        public bool DisableAlerts;
        public bool OverrideBuildRoof;
        public bool FactionRemovalMode;
        public bool ReplaceIngredientFinder;
        public bool OptimiseJobGiverOptimise;
        public bool DynamicSpeedControl = false;
        public bool ShowOnMainTab = true;
        public bool AdvancedMode = false;
        public bool DevMode = false;
        public bool SnowOptimize = false;
        public bool SidePanel = false;

        //  public bool MuteGC = false;
        public int CurrentTab = 0;
        public string @PathToDnspy = "";
        public override void ExposeData()
        {
            base.ExposeData();

            /* Performace Options  */
            Scribe_Values.Look(ref MeshOnlyBuildings, "MeshOnlyBuildings");
            Scribe_Values.Look(ref ShowOnMainTab, "ShowOnMainTab");
            Scribe_Values.Look(ref FixRepair, "FixRepair");
            Scribe_Values.Look(ref FixGame, "FixGame");
            Scribe_Values.Look(ref SnowOptimize, "SnowOptimize");
            Scribe_Values.Look(ref OptimizeDrills, "OptimizeDrills");
            Scribe_Values.Look(ref UnlockFramerate, "UnlockFramerate");
            Scribe_Values.Look(ref OptimizeDrawInspectGizmoGrid, "OptimizeDrawInspectGizmoGrid");
            Scribe_Values.Look(ref NeverCheckJobsOnDamage, "NeverCheckJobsOnDamage");
            Scribe_Values.Look(ref HumanoidOnlyWarden, "HumanoidOnlyWarden");
            Scribe_Values.Look(ref OverrideBuildRoof, "OverrideBuildRoof");
            Scribe_Values.Look(ref OptimizeAlerts, "OptimizeAlerts");
            Scribe_Values.Look(ref OverrideAlerts, "OverrideAlerts");
            Scribe_Values.Look(ref DisableAlerts, "DisableAlerts");
            Scribe_Values.Look(ref KillMusicMan, "KillMusicMan");
            Scribe_Values.Look(ref OptimiseJobGiverOptimise, "OptimiseJobGiverOptimise");
            Scribe_Values.Look(ref DynamicSpeedControl, "DynamicSpeedControl");

            /* Cosmetic Options */
            Scribe_Values.Look(ref SidePanel, "SidePanel");
            Scribe_Values.Look(ref LineCol, "LineCol", new Color32(79, 147, 191, 255));
            Scribe_Values.Look(ref GraphCol, "GraphCol", new Color32(17, 17, 17, 255));

            /* Levels of access into the analyzer */
            Scribe_Values.Look(ref AdvancedMode, "AdvancedMode");
            Scribe_Values.Look(ref DevMode, "DevMode");


            Scribe_Values.Look(ref PathToDnspy, "PathToDnspy");

            //  Scribe_Values.Look(ref MuteGC, "MuteGC");
            //    Scribe_Collections.Look(ref Loggers, "Loggers");
            //  Scribe_Values.Look(ref ReplaceIngredientFinder, "ReplaceIngredientFinder", false);
            // Scribe_Values.Look(ref FixBedMemLeak, "FixBedMemLeak");

            try
            {
                Scribe_Collections.Look(ref AlertFilter, "AlertFilter");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }

        private static Vector2 scrollPos;
        public static float settingsHeight = 500;

        public void DoSettings(Rect canvas)
        {
            if (Event.current.type == EventType.Layout) return;

            if (AdvancedMode)
            {
                canvas.y += 35f;
                //Rect rect = canvas.LeftPart(.75f);
                List<TabRecord> list = new List<TabRecord>();
                list.Add(new TabRecord("settings.tabs.performance".Translate(), delegate { CurrentTab = 0; Write(); }, CurrentTab == 0));
                list.Add(new TabRecord("settings.tabs.developertools".Translate(), delegate { CurrentTab = 1; Write(); }, CurrentTab == 1));

                TabDrawer.DrawTabs(canvas, list, 500f);
            }

            var view = canvas.AtZero();
            view.height = settingsHeight;
            Widgets.BeginScrollView(canvas, ref scrollPos, view, false);
            GUI.BeginGroup(view);
            view.height = 9999;
            listing.Begin(view.ContractedBy(10f));

            { // Draw the github and discord textures / Icons
                if (80 + "PerfAnalWiki".Translate().GetWidthCached() + "DubModDisco".Translate().GetWidthCached() < listing.ColumnWidth)
                {
                    var rec = listing.GetRect(24f);
                    var lrec = rec.LeftHalf();
                    rec = rec.RightHalf();
                    Widgets.DrawTextureFitted(lrec.LeftPartPixels(40f), Gfx.Support, 1f);
                    lrec.x += 40;
                    if (Widgets.ButtonText(lrec.LeftPartPixels("PerfAnalWiki".Translate().GetWidthCached()), "PerfAnalWiki".Translate(), false, true))
                    {
                        Application.OpenURL("https://github.com/Dubwise56/Dubs-Performance-Analyzer/wiki");
                    }

                    Widgets.DrawTextureFitted(rec.RightPartPixels(40f), Gfx.disco, 1f);
                    rec.width -= 40;
                    if (Widgets.ButtonText(rec.RightPartPixels("DubModDisco".Translate().GetWidthCached()), "DubModDisco".Translate(), false, true))
                    {
                        Application.OpenURL("https://discord.gg/Az5CnDW");
                    }
                }
                else
                {
                    var rec = listing.GetRect(24f);
                    Widgets.DrawTextureFitted(rec.LeftPartPixels(40f), Gfx.Support, 1f);
                    if (Widgets.ButtonText(rec.RightPartPixels(rec.width - 40), "PerfAnalWiki".Translate(), false, true))
                    {
                        Application.OpenURL("https://github.com/Dubwise56/Dubs-Performance-Analyzer/wiki");
                    }

                    rec = listing.GetRect(24f);
                    Widgets.DrawTextureFitted(rec.LeftPartPixels(40f), Gfx.disco, 1f);
                    if (Widgets.ButtonText(rec.RightPartPixels(rec.width - 40), "DubModDisco".Translate(), false, true))
                    {
                        Application.OpenURL("https://discord.gg/Az5CnDW");
                    }
                }
            }

            listing.GapLine();
            if (CurrentTab == 0)
            {
                DrawPerformanceOptions();
            }
            else
            {
                DrawDevOptions();
            }
            listing.GapLine();

            settingsHeight = listing.GetRect(25).yMax;

            listing.End();
            GUI.EndGroup();
            Widgets.EndScrollView();
        }

        public static UpdateMode customPatchMode = UpdateMode.Tick;

        public void DrawPerformanceOptions()
        {
            listing.Label("settings.performance.heading".Translate());


            DubGUI.Checkbox("TempSpeedup".Translate(), listing, ref Analyzer.Settings.FixGame);
            //   DubGUI.Checkbox("Fix memory leak on beds and room stats", listing, ref Analyzer.Settings.FixBedMemLeak);
            DubGUI.Checkbox("RoofOptimize".Translate(), listing, ref OverrideBuildRoof);

            if (DubGUI.Checkbox("RepairOptimize".Translate(), listing, ref FixRepair))
            {
                if (Current.ProgramState == ProgramState.Playing)
                {
                    foreach (var gameMap in Current.Game.Maps)
                    {
                        foreach (var k in gameMap.listerBuildingsRepairable.repairables.Keys)
                        {
                            var bs = gameMap.listerBuildingsRepairable.repairables[k];
                            foreach (var b in bs.ToList())
                            {
                                if (!b.def.building.repairable || !b.def.useHitPoints)
                                {
                                    gameMap.listerBuildingsRepairable.repairables[k].Remove(b);
                                }
                            }
                        }

                        foreach (var k in gameMap.listerBuildingsRepairable.repairablesSet.Keys)
                        {
                            var bs = gameMap.listerBuildingsRepairable.repairables[k];
                            foreach (var b in bs.ToList())
                            {
                                if (!b.def.building.repairable || !b.def.useHitPoints)
                                {
                                    gameMap.listerBuildingsRepairable.repairables[k].Remove(b);
                                }
                            }
                        }
                    }
                }
            }
            DubGUI.Checkbox("SnowOptimize".Translate(), listing, ref SnowOptimize);
            DubGUI.Checkbox("OptimizeDrills".Translate(), listing, ref OptimizeDrills);
            DubGUI.Checkbox("OptimizeAlerts".Translate(), listing, ref OptimizeAlerts);
            DubGUI.Checkbox("JobGiverOptimise".Translate(), listing, ref OptimiseJobGiverOptimise);

            DubGUI.Checkbox("GizmoOpti".Translate(), listing, ref OptimizeDrawInspectGizmoGrid);
            var jam = Analyzer.Settings.MeshOnlyBuildings;
            DubGUI.Checkbox("RealtimeCondu".Translate(), listing, ref MeshOnlyBuildings);
            if (jam != Analyzer.Settings.MeshOnlyBuildings)
            {
                H_FixWallsNConduits.Swapclasses();
            }
            DubGUI.Checkbox("DamageJobRecheck".Translate(), listing, ref NeverCheckJobsOnDamage);

            listing.GapLine();
            listing.Label("settings.performance.destructiveheading".Translate());
            DubGUI.Checkbox("OverrideAlerts".Translate(), listing, ref OverrideAlerts);
            DubGUI.Checkbox("KillMusicMan".Translate(), listing, ref KillMusicMan);
            DubGUI.Checkbox("DisableAlerts".Translate(), listing, ref DisableAlerts);
            listing.GapLine();
            listing.Label("settings.experimental.header".Translate());
            DubGUI.Checkbox("DynamicSpeedControl".Translate(), listing, ref DynamicSpeedControl);
            listing.GapLine();
            listing.Label("settings.performance.generalheading".Translate());
            DubGUI.Checkbox("FactionRemovalMode".Translate(), listing, ref FactionRemovalMode);
            DubGUI.Checkbox("ShowAnalBut".Translate(), listing, ref ShowOnMainTab);
            //   DubGUI.Checkbox("Mute GC messages", listing, ref Analyzer.Settings.MuteGC);
            if (DubGUI.Checkbox("UnlockFramerate".Translate(), listing, ref UnlockFramerate))
            {
                if (UnlockFramerate)
                {
                    QualitySettings.vSyncCount = 0;
                    Application.targetFrameRate = -1;
                }
                else
                {
                    QualitySettings.vSyncCount = 1;
                    Application.targetFrameRate = 60;
                }
            }

            DubGUI.Checkbox("AdvProfMode".Translate(), listing, ref AdvancedMode);

            //  dirk("Replace bill ingredient finder (Testing only)", ref Analyzer.Settings.ReplaceIngredientFinder);
            //var dan = Analyzer.Settings.HumanoidOnlyWarden;
            //dirk("Replace warden jobs to only scan Humanoids (Testing only)", ref Analyzer.Settings.HumanoidOnlyWarden);
            //if (dan != Analyzer.Settings.HumanoidOnlyWarden)
            //{
            //    H_WardenRequest.Swapclasses();
            //}
        }



        /* For Dev Tools */

        public enum CurrentInput { Method, Type, MethodHarmony, TypeHarmony, InternalMethod, Assembly }
        public enum UnPatchType { Method, MethodsOnMethod, Type, InternalMethod, All }

        public static CurrentInput input = CurrentInput.Method;
        public static UnPatchType unPatchType = UnPatchType.Method;
        public static UpdateMode patchType = UpdateMode.Update;

        public static string currentInput = null;
        public static string currentUnPatch = null;
        public static string currentWIP = null;

        public static HashSet<string> cachedEntries = new HashSet<string>();
        public static bool curSearching = false;
        public static Thread searchThread = null;
        public static object sync = new object();
        public static string prevInput = "";

        public void DrawDevOptions()
        {
            listing.Label("settings.developer.heading".Translate());
            DubGUI.Checkbox("SidePanel".Translate(), listing, ref SidePanel);
            DubGUI.Checkbox("TickPawnTog".Translate(), listing, ref H_PawnTick.TickPawns);
            DubGUI.Checkbox("DevMode".Translate(), listing, ref DevMode);
            if (DevMode)
            {
                DubGUI.InputField(listing.GetRect(Text.LineHeight), "Path to Dnspy.exe (including the exe)", ref PathToDnspy, ShowName: true);
            }

            listing.GapLine();

            var left = listing.GetRect(Text.LineHeight * 11); // the average height of this is ~226, which is 10.2 * Text.LineHeight
            var right = left.RightPart(0.48f);
            left = left.LeftPart(0.48f);

            DrawPatches(left);
            DrawUnPatches(right);

            DubGUI.CenterText(() => listing.Label("Utilites"));
        }

        public void DrawPatches(Rect left)
        {
            var lListing = new Listing_Standard();
            lListing.Begin(left);

            DubGUI.CenterText(() => lListing.Label("ProfilePatchMethod".Translate()));

            lListing.GapLine(6);
            DubGUI.OptionalBox(lListing.GetRect(Text.LineHeight + 3), "input.method".Translate(), () => input = CurrentInput.Method, input == CurrentInput.Method);
            DubGUI.OptionalBox(lListing.GetRect(Text.LineHeight + 3), "input.type".Translate(), () => input = CurrentInput.Type, input == CurrentInput.Type);
            DubGUI.OptionalBox(lListing.GetRect(Text.LineHeight + 3), "input.methodharmony".Translate(), () => input = CurrentInput.MethodHarmony, input == CurrentInput.MethodHarmony);
            DubGUI.OptionalBox(lListing.GetRect(Text.LineHeight + 3), "input.typeharmony".Translate(), () => input = CurrentInput.TypeHarmony, input == CurrentInput.TypeHarmony);
            DubGUI.OptionalBox(lListing.GetRect(Text.LineHeight + 3), "input.methodinternal".Translate(), () => input = CurrentInput.InternalMethod, input == CurrentInput.InternalMethod);
            DubGUI.OptionalBox(lListing.GetRect(Text.LineHeight + 3), "input.assembly".Translate(), () => input = CurrentInput.Assembly, input == CurrentInput.Assembly);
            lListing.curY += 2;

            DisplayInputField(lListing);
            lListing.curY += 2;

            var box = lListing.GetRect(Text.LineHeight + 3);

            DubGUI.OptionalBox(box.LeftPart(.3f), "patch.type.tick".Translate(), () => patchType = UpdateMode.Tick, patchType == UpdateMode.Tick);
            box = box.RightPart(.65f);
            DubGUI.OptionalBox(box.LeftPart(.4f), "patch.type.update".Translate(), () => patchType = UpdateMode.Update, patchType == UpdateMode.Update);

            if (Widgets.ButtonText(box.RightPart(.5f), "patch".Translate()))
                if (currentInput != null)
                    ExecutePatch();

            lListing.End();
        }

        public static void DisplayInputField(Listing_Standard listing)
        {
            string FieldDescription = null;

            switch (input)
            {
                case CurrentInput.Method: FieldDescription = "Type:Method"; break;
                case CurrentInput.Type: FieldDescription = "Type"; break;
                case CurrentInput.MethodHarmony: FieldDescription = "Type:Method"; break;
                case CurrentInput.TypeHarmony: FieldDescription = "Type"; break;
                case CurrentInput.InternalMethod: FieldDescription = "Type:Method"; break;
                case CurrentInput.Assembly: FieldDescription = "Mod or PackageId"; break;
            }

            Rect inputBox = listing.GetRect(Text.LineHeight);
            DubGUI.InputField(inputBox, FieldDescription, ref currentInput, ShowName: true);
            PopulateSearch(inputBox, currentInput, input);
        }
        public void ExecutePatch()
        {
            switch (input)
            {
                case CurrentInput.Method:
                    if (patchType == UpdateMode.Tick)
                    {
                        CustomProfilersTick.PatchMeth(currentInput);
                        AnalyzerState.SwapTab("Custom Tick", UpdateMode.Tick);
                    }
                    else
                    {
                        CustomProfilersUpdate.PatchMeth(currentInput);
                        AnalyzerState.SwapTab("Custom Update", UpdateMode.Update);
                    }
                    return;
                case CurrentInput.Type:
                    CustomProfilersUpdate.PatchType(currentInput);
                    AnalyzerState.SwapTab("Custom Update", UpdateMode.Update);
                    return;
                case CurrentInput.MethodHarmony:
                    CustomProfilersHarmony.PatchMeth(currentInput);
                    AnalyzerState.SwapTab("Custom Harmony", UpdateMode.Update);
                    return;
                case CurrentInput.TypeHarmony:
                    CustomProfilersHarmony.PatchType(currentInput);
                    AnalyzerState.SwapTab("Custom Harmony", UpdateMode.Update);
                    return;
                case CurrentInput.InternalMethod:
                    PatchUtils.PatchInternalMethod(currentInput);
                    return;
                case CurrentInput.Assembly:
                    PatchUtils.PatchAssembly(currentInput, false);
                    return;
            }
        }
        public void DrawUnPatches(Rect right)
        {
            var rListing = new Listing_Standard();
            rListing.Begin(right);

            DubGUI.CenterText(() => rListing.Label("UnProfilePatchMethod".Translate()));
            rListing.GapLine(6);
            DubGUI.OptionalBox(rListing.GetRect(Text.LineHeight + 3), "input.unpatchmethod".Translate(), () => unPatchType = UnPatchType.Method, unPatchType == UnPatchType.Method);
            DubGUI.OptionalBox(rListing.GetRect(Text.LineHeight + 3), "input.unpatchmethodsonmethod".Translate(), () => unPatchType = UnPatchType.MethodsOnMethod, unPatchType == UnPatchType.MethodsOnMethod);
            DubGUI.OptionalBox(rListing.GetRect(Text.LineHeight + 3), "input.unpatchtype".Translate(), () => unPatchType = UnPatchType.Type, unPatchType == UnPatchType.Type);
            DubGUI.OptionalBox(rListing.GetRect(Text.LineHeight + 3), "input.unpatchinternalmethod".Translate(), () => unPatchType = UnPatchType.InternalMethod, unPatchType == UnPatchType.InternalMethod);
            DubGUI.OptionalBox(rListing.GetRect(Text.LineHeight + 3), "input.unpatchall".Translate(), () => unPatchType = UnPatchType.All, unPatchType == UnPatchType.All);
            rListing.curY += 2;

            DisplayUnPatchInputField(rListing);
            rListing.curY += 2;
            if (Widgets.ButtonText(rListing.GetRect(Text.LineHeight + 3), "unpatch".Translate()))
                if (currentInput != null || unPatchType == UnPatchType.All)
                    ExecuteUnPatch();

            rListing.End();
        }

        public static void DisplayUnPatchInputField(Listing_Standard listing)
        {
            string FieldDescription = null;

            switch (unPatchType)
            {
                case UnPatchType.Method: FieldDescription = "Type:Method"; break;
                case UnPatchType.MethodsOnMethod: FieldDescription = "Type:Method"; break;
                case UnPatchType.Type: FieldDescription = "Type"; break;
                case UnPatchType.InternalMethod: FieldDescription = "Type:Method"; break;
                case UnPatchType.All: FieldDescription = "N/A"; break;
            }

            Rect inputBox = listing.GetRect(Text.LineHeight);
            DubGUI.InputField(inputBox, FieldDescription, ref currentUnPatch, ShowName: true);
        }

        public static void ExecuteUnPatch()
        {
            switch (unPatchType)
            {
                case UnPatchType.Method: PatchUtils.UnpatchMethod(currentUnPatch); break;
                case UnPatchType.MethodsOnMethod: PatchUtils.UnpatchMethod(currentUnPatch); break;
                case UnPatchType.Type: PatchUtils.UnPatchTypePatches(currentUnPatch); break;
                case UnPatchType.InternalMethod: PatchUtils.UnpatchInternalMethod(currentUnPatch); break;
                case UnPatchType.All: Analyzer.UnPatchMethods(true); break;
            }
        }


        public static void PopulateSearch(Rect rect, string searchText, CurrentInput inputType)
        {
            bool active = false;
            lock (sync)
            {
                active = curSearching;
            }

            if (!active && prevInput != currentInput)
            {
                switch (inputType)
                {
                    case CurrentInput.Method:
                    case CurrentInput.InternalMethod:
                    case CurrentInput.MethodHarmony:
                        searchThread = new Thread(() => PopulateSearchMethod(searchText));
                        break;
                    case CurrentInput.Type:
                    case CurrentInput.TypeHarmony:
                        searchThread = new Thread(() => PopulateSearchType(searchText));
                        break;
                    default:
                        searchThread = new Thread(() => PopulateSearchAssembly(searchText));
                        break;

                }
                searchThread.IsBackground = true;
                prevInput = currentInput;
                searchThread.Start();
            }

            lock (sync)
            {
                if (cachedEntries.Count != 0)
                {
                    DropDownSearchMenu.DoWindowContents(rect);
                }
            }
        }

        private static void PopulateSearchMethod(string searchText)
        {
            if (searchText.Length <= 4) return;

            lock (sync)
            {
                curSearching = true;
            }

            var names = new HashSet<string>();

            foreach (var type in GenTypes.AllTypes)
            {
                if (type.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() == null && !type.FullName.Contains("DubsAnalyzer"))
                {
                    foreach (var meth in type.GetMethods())
                    {
                        if (meth.DeclaringType == type && !meth.IsSpecialName && !meth.IsAssembly && meth.HasMethodBody())
                        {
                            var str = string.Concat(meth.DeclaringType, ":", meth.Name);
                            if (str.Contains(searchText))
                                names.Add(str);
                        }
                    }
                }
            }


            lock (sync)
            {
                cachedEntries = names;
                curSearching = false;
            }
        }

        private static void PopulateSearchType(string searchText)
        {
            if (searchText.Length <= 2) return;

            lock (sync)
            {
                curSearching = true;
            }

            var names = new HashSet<string>();
            foreach (var type in GenTypes.AllTypes)
            {
                if (type.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() == null)
                {
                    if (type.FullName.Contains(searchText) && !type.FullName.Contains("DubsAnalyzer"))
                        names.Add(type.FullName);
                }
            }
            lock (sync)
            {
                cachedEntries = names;
                curSearching = false;
            }
        }

        private static void PopulateSearchAssembly(string searchText)
        {
            lock (sync)
            {
                curSearching = true;
            }

            var names = new HashSet<string>();
            foreach (var mod in AnalyzerCache.AssemblyToModname.Values)
            {
                if (mod.ToLower().Contains(searchText.ToLower()))
                    names.Add(mod);
            }
            lock (sync)
            {
                cachedEntries = names;
                curSearching = false;
            }
        }
    }
}