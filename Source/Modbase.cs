﻿using HarmonyLib;
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

        public Modbase(ModContentPack content) : base(content)
        {
            Settings = GetSettings<Settings>();

            ModInfoCache.PopulateCache(Content.Name);
            XmlParser.CollectXmlData();
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