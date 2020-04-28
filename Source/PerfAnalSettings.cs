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
        public bool OptimiseAlerts;
        public bool OptimizeDrills;
        public bool DisableAlerts;
        public bool OverrideBuildRoof;
        public bool ReplaceIngredientFinder;
        public bool ShowOnMainTab = true;
        public bool AdvancedMode = false;
        //  public bool MuteGC = false;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref LineCol, "LineCol", new Color32(79, 147, 191, 255));
            Scribe_Values.Look(ref GraphCol, "GraphCol", new Color32(17, 17, 17, 255));
            Scribe_Values.Look(ref AdvancedMode, "AdvancedMode");
            Scribe_Values.Look(ref MeshOnlyBuildings, "MeshOnlyBuildings");
            Scribe_Values.Look(ref ShowOnMainTab, "ShowOnMainTab");
            Scribe_Values.Look(ref FixRepair, "FixRepair");
            Scribe_Values.Look(ref FixGame, "FixGame");
            Scribe_Values.Look(ref OptimizeDrills, "OptimizeDrills");
            Scribe_Values.Look(ref UnlockFramerate, "UnlockFramerate");
            //  Scribe_Values.Look(ref ReplaceIngredientFinder, "ReplaceIngredientFinder", false);
            // Scribe_Values.Look(ref FixBedMemLeak, "FixBedMemLeak");
            Scribe_Values.Look(ref OptimizeDrawInspectGizmoGrid, "OptimizeDrawInspectGizmoGrid");
            Scribe_Values.Look(ref NeverCheckJobsOnDamage, "NeverCheckJobsOnDamage");
            Scribe_Values.Look(ref HumanoidOnlyWarden, "HumanoidOnlyWarden");
            Scribe_Values.Look(ref OverrideBuildRoof, "OverrideBuildRoof");
            Scribe_Values.Look(ref OptimiseAlerts, "OptimiseAlerts");
            Scribe_Values.Look(ref OverrideAlerts, "OverrideAlerts");
            Scribe_Values.Look(ref DisableAlerts, "DisableAlerts");
            Scribe_Values.Look(ref KillMusicMan, "KillMusicMan");
            //  Scribe_Values.Look(ref MuteGC, "MuteGC");
            //    Scribe_Collections.Look(ref Loggers, "Loggers");

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


        public void DoSettings(Rect canvas)
        {
            listing.Begin(canvas.ContractedBy(10f));

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

            listing.GapLine();
            listing.Label("OptimizationsAndFixes".Translate());
            DubGUI.Checkbox("TempSpeedup".Translate(), listing,
                ref Analyzer.Settings.FixGame);
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
            DubGUI.Checkbox("OptimizeDrills".Translate(), listing, ref OptimizeDrills);
            DubGUI.Checkbox("OptimiseAlerts".Translate(), listing, ref OptimiseAlerts);
            
            DubGUI.Checkbox("GizmoOpti".Translate(), listing, ref OptimizeDrawInspectGizmoGrid);
            var jam = Analyzer.Settings.MeshOnlyBuildings;
            DubGUI.Checkbox("RealtimeCondu".Translate(), listing, ref MeshOnlyBuildings);
            if (jam != Analyzer.Settings.MeshOnlyBuildings)
            {
                H_FixWallsNConduits.Swapclasses();
            }
            // dirk("Never check jobs on take damage", ref Analyzer.Settings.NeverCheckJobsOnDamage);
            listing.GapLine();
            DubGUI.Checkbox("OverrideAlerts".Translate(), listing, ref OverrideAlerts);
            listing.GapLine();
            DubGUI.Checkbox("KillMusicMan".Translate(), listing, ref KillMusicMan);
            DubGUI.Checkbox("DisableAlerts".Translate(), listing, ref DisableAlerts);
            //  dirk("Replace bill ingredient finder (Testing only)", ref Analyzer.Settings.ReplaceIngredientFinder);
            //var dan = Analyzer.Settings.HumanoidOnlyWarden;
            //dirk("Replace warden jobs to only scan Humanoids (Testing only)", ref Analyzer.Settings.HumanoidOnlyWarden);
            //if (dan != Analyzer.Settings.HumanoidOnlyWarden)
            //{
            //    H_WardenRequest.Swapclasses();
            //}
            listing.GapLine();
            DubGUI.Checkbox("ShowAnalBut".Translate(), listing, ref ShowOnMainTab);
            //   DubGUI.Checkbox("Mute GC messages", listing, ref Analyzer.Settings.MuteGC);
            DubGUI.Checkbox("AdvProfMode".Translate(), listing, ref AdvancedMode);

            if (DubGUI.Checkbox("Unlock framerate", listing, ref UnlockFramerate))
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

            if (AdvancedMode)
            {
                DubGUI.Checkbox("TickPawnTog".Translate(), listing, ref H_PawnTick.TickPawns);

                listing.GapLine();

                listing.Label("CustoMethProfPatch".Translate());
                var r = listing.GetRect(25f);
                DubGUI.InputField(r, "Type:Method", ref methToPatch, ShowName: true);
                r = listing.GetRect(25f).LeftPartPixels(150);
                if (Widgets.RadioButtonLabeled(r, "CustoTickPatch".Translate(), customPatchMode == UpdateMode.Tick))
                {
                    customPatchMode = UpdateMode.Tick;
                }
                r = listing.GetRect(25f).LeftPartPixels(150);
                if (Widgets.RadioButtonLabeled(r, "CustoUpdatePatch".Translate(), customPatchMode == UpdateMode.Update))
                {
                    customPatchMode = UpdateMode.Update;
                }
                var b = listing.GetRect(25);
                if (Widgets.ButtonText(b.LeftPartPixels(100), "TryCustoPatch".Translate()))
                {
                    if (customPatchMode == UpdateMode.Tick)
                    {
                        CustomProfilersTick.PatchMeth(methToPatch);
                    }
                    else
                    {
                        CustomProfilersUpdate.PatchMeth(methToPatch);
                    }
                }
            }


            listing.End();
        }

        public static UpdateMode customPatchMode = UpdateMode.Tick;



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

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.EndScrollView();
        }
    }
}