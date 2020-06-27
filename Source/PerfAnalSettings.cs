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

            Dialog_DeveloperSettings.DrawOptions(listing);

        }

    }
}