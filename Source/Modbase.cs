using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;
using UnityEngine;
using Verse;

namespace Analyzer
{
    public class Modbase : Mod
    {
        public static Settings Settings;
        private static Harmony harmony = null;
        public static Harmony Harmony => harmony ??= new Harmony("Dubwise.DubsProfiler");
        private static Harmony staticHarmony = null;
        public static Harmony StaticHarmony => staticHarmony ??= new Harmony("Dubswise.PerformanceAnalyzer");

        public static bool isPatched = false;

        public Modbase(ModContentPack content) : base(content)
        {
            Settings = GetSettings<Settings>();

            { // Profiling
                ModInfoCache.PopulateCache(Content.Name);

                Profiling.GUIController.InitialiseTabs();

                // GUI needs to be initialised before xml (the tabs need to exist for entries to be inserted into them)
                Profiling.XmlParser.CollectXmlData();
            }

            { // Always Running
                StaticHarmony.Patch(AccessTools.Method(typeof(GlobalControlsUtility), nameof(GlobalControlsUtility.DoTimespeedControls)), 
                    prefix: new HarmonyMethod(typeof(GUIElement_TPS), nameof(GUIElement_TPS.Prefix)));
            }
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoSettings(inRect);
        }

        public override string SettingsCategory()
        {
            return "Dubs Performance Analyzer";
        }
    }
}