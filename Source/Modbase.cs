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

        private static void ThreadStart(Dictionary<string, Profiler> Profiles)
        {
            List<ProfileLog> newLogs = new List<ProfileLog>();
            foreach (string key in Profiles.Keys)
            {
                Profiler value = Profiles[key];
                double av = value.GetAverageTime(Mathf.Min(Analyzer.GetCurrentLogCount, 2000));
                newLogs.Add(new ProfileLog(value.label, string.Empty, av, (float)value.times.Max(), null, key, string.Empty, 0, value.type, value.meth));
            }

            double total = newLogs.Sum(x => x.Average);

            for (int index = 0; index < newLogs.Count; index++)
            {
                ProfileLog k = newLogs[index];
                float pc = (float)(k.Average / total);
                ProfileLog Log = new ProfileLog(k.Label, pc.ToStringPercent(), pc, k.Max, k.Def, k.Key, k.Mod, pc, k.Type, k.Meth);
                newLogs[index] = Log;
            }

            newLogs.SortByDescending(x => x.Average);

            // Swap our old logs with the new ones
        }
    }
}