using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        private static float groaner = 9999999;
        private static Vector2 scrolpos = Vector2.zero;

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

        public enum CurrentInput { Method, Type, MethodHarmony, TypeHarmony }
        public enum UnPatchType { Method, MethodsOnMethod, Type, All }
        public enum WIPType { Assembly, InternalMethod }

        public static CurrentInput input = CurrentInput.Method;
        public static UnPatchType unPatchType = UnPatchType.Method;
        public static UpdateMode patchType = UpdateMode.Update;
        public static WIPType wipType = WIPType.Assembly;

        public static string currentInput = null;
        public static string currentUnPatch = null;
        public static string currentWIP = null;

        public void DrawDevOptions()
        {
            listing.Label("settings.developer.heading".Translate());
            DubGUI.Checkbox("SidePanel".Translate(), listing, ref SidePanel);
            DubGUI.Checkbox("TickPawnTog".Translate(), listing, ref H_PawnTick.TickPawns);
            DubGUI.Checkbox("DevMode".Translate(), listing, ref DevMode);
            if(DevMode)
            {
                DubGUI.InputField(listing.GetRect(Text.LineHeight), "Path to Dnspy.exe (including the exe)", ref PathToDnspy, ShowName: true);
            }

            listing.GapLine();

            var left = listing.GetRect(Text.LineHeight * 9); // the average height of this is ~181, which is 8.2 * Text.LineHeight
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
            DubGUI.OptionalBox(lListing.GetRect(Text.LineHeight + 3), "input.method".Translate(), delegate { input = CurrentInput.Method; }, input == CurrentInput.Method);
            DubGUI.OptionalBox(lListing.GetRect(Text.LineHeight + 3), "input.type".Translate(), delegate { input = CurrentInput.Type; }, input == CurrentInput.Type);
            DubGUI.OptionalBox(lListing.GetRect(Text.LineHeight + 3), "input.methodharmony".Translate(), delegate { input = CurrentInput.MethodHarmony; }, input == CurrentInput.MethodHarmony);
            DubGUI.OptionalBox(lListing.GetRect(Text.LineHeight + 3), "input.typeharmony".Translate(), delegate { input = CurrentInput.TypeHarmony; }, input == CurrentInput.TypeHarmony);
            lListing.curY += 2;

            DisplayInputField(lListing);
            lListing.curY += 2;

            var box = lListing.GetRect(Text.LineHeight + 3);

            DubGUI.OptionalBox(box.LeftPart(.3f), "patch.type.tick".Translate(), delegate { patchType = UpdateMode.Tick; }, patchType == UpdateMode.Tick);
            box = box.RightPart(.65f);
            DubGUI.OptionalBox(box.LeftPart(.4f), "patch.type.update".Translate(), delegate { patchType = UpdateMode.Update; }, patchType == UpdateMode.Update);
            
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
            }

            Rect inputBox = listing.GetRect(Text.LineHeight);
            DubGUI.InputField(inputBox, FieldDescription, ref currentInput, ShowName: true);
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
            }
        }
        public void DrawUnPatches(Rect right)
        {
            var rListing = new Listing_Standard();
            rListing.Begin(right);

            DubGUI.CenterText(() => rListing.Label("UnProfilePatchMethod".Translate()));

            rListing.GapLine(6);
            DubGUI.OptionalBox(rListing.GetRect(Text.LineHeight + 3), "input.unpatchmethod".Translate(), delegate { unPatchType = UnPatchType.Method; }, unPatchType == UnPatchType.Method);
            DubGUI.OptionalBox(rListing.GetRect(Text.LineHeight + 3), "input.unpatchmethodsonmethod".Translate(), delegate { unPatchType = UnPatchType.MethodsOnMethod; }, unPatchType == UnPatchType.MethodsOnMethod);
            DubGUI.OptionalBox(rListing.GetRect(Text.LineHeight + 3), "input.unpatchtype".Translate(), delegate { unPatchType = UnPatchType.Type; }, unPatchType == UnPatchType.Type);
            DubGUI.OptionalBox(rListing.GetRect(Text.LineHeight + 3), "input.unpatchall".Translate(), delegate { unPatchType = UnPatchType.All; }, unPatchType == UnPatchType.All);
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
                case UnPatchType.All: Analyzer.unPatchMethods(true); break;
            }
        }







        public static void TypesBox(Rect rect)
        {
            var speng = rect;
            speng.height = 25f;
            var glorph = speng.LeftPart(0.9f);
            speng = speng.RightPart(0.1f);

            var reod = false;
            var old1 = TypeSearch;
            TypeSearch = Widgets.TextField(glorph.LeftHalf(), TypeSearch);
            if (old1 != TypeSearch)
            {
                reod = true;
            }

            var old2 = MethSearch;
            MethSearch = Widgets.TextField(glorph.RightHalf(), MethSearch);
            if (old2 != MethSearch)
            {
                reod = true;
            }

            //if (Widgets.ButtonText(speng, "Search"))
            //{

            //}

            if (reod && (TypeSearch.Length > 2 || MethSearch.Length > 2))
            {
                GotMeth = Dialog_Analyzer.SearchFor().ToList();
            }


            rect.y += 25f;
            rect.height -= 25f;

            var innyrek = rect.AtZero();
            innyrek.width -= 32f;
            innyrek.height = groaner;

            Widgets.BeginScrollView(rect, ref scrolpos, innyrek);

            GUI.BeginGroup(innyrek);

            listing.Begin(innyrek);

            float goat = 0;

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Tiny;

            var coo = 0;
            foreach (var meth in GotMeth)
            {
                coo++;
                if (coo == 50)
                {
                    break;
                }

                var tp = meth.Name;

                var r = listing.GetRect(30f);

                if (Analyzer.Settings.Loggers == null)
                {
                    Analyzer.Settings.Loggers = new Dictionary<string, bool>();
                }

                if (Widgets.ButtonInvisible(r))
                {
                    if (!Analyzer.Settings.Loggers.ContainsKey(tp))
                    {
                        Analyzer.Settings.Loggers.Add(tp, true);
                    }
                    else
                    {
                        var bam = Analyzer.Settings.Loggers[tp];
                        Analyzer.Settings.Loggers[tp] = !bam;
                    }
                }

                if (Analyzer.Settings.Loggers.ContainsKey(tp))
                {
                    var bam = Analyzer.Settings.Loggers[tp];

                    r = r.LeftPartPixels(75);
                    Widgets.CheckboxDraw(r.x, r.y, bam, true);
                }
                else
                {
                    r = r.LeftPartPixels(75);
                    Widgets.CheckboxDraw(r.x, r.y, false, true);
                }

                r.x = r.xMax;
                r.width = 2000;
                Widgets.Label(r, $"{meth.ReflectedType} {meth}");

                listing.GapLine(0f);
                goat += 4f;
                goat += r.height;
            }

            listing.End();
            groaner = goat;
            GUI.EndGroup();

            DubGUI.ResetFont();
            Widgets.EndScrollView();
        }
    }
}