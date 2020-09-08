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
        public static bool isPatched = true;

        public Modbase(ModContentPack content) : base(content)
        {
            Settings = GetSettings<Settings>();

            ModInfoCache.PopulateCache(Content.Name);

            GUIController.InitialiseTabs();
            
            // GUI needs to be initialised before xml (the tabs need to exist for entries to be inserted into them)
            XmlParser.CollectXmlData();

            Harmony.PatchAll();
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