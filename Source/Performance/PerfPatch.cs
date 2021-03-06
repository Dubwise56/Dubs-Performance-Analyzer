﻿using System;
using System.Reflection;
using Analyzer.Profiling;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Analyzer.Performance
{
    public enum PerformanceCategory
    {
        Optimizes,
        Overrides,
        Removes
    }

    public class PerfPatch
    {
        public AccessTools.FieldRef<bool> EnabledRefAccess;
        public bool isPatched;
        public virtual string Name => "";
        public virtual PerformanceCategory Category => PerformanceCategory.Optimizes;

        public void Initialise(Type subType)
        {
            EnabledRefAccess =
                AccessTools.StaticFieldRefAccess<bool>(subType.GetField("Enabled",
                    BindingFlags.Public | BindingFlags.Static));
            if (EnabledRefAccess == null)
            {
                ThreadSafeLogger.Error("Add an 'Enabled' field you bloody muppet");
            }
        }

        public virtual void Draw(Listing_Standard listing)
        {
            var name = Name.Tr();
            var tooltip = (Name + ".tooltip").Tr();

          //  var height = Mathf.CeilToInt((name.GetWidthCached() + 30) / listing.ColumnWidth) * Text.LineHeight;
            var rect = listing.GetRect(Text.LineHeight);

            if (DubGUI.Checkbox(rect, name, ref EnabledRefAccess()))
            {
                CheckState();
            }

            TooltipHandler.TipRegion(rect, tooltip);
        }

        public void CheckState()
        {
            if (EnabledRefAccess())
            {
                if (PerformancePatches.onDisabled.ContainsKey(Name))
                {
                    PerformancePatches.onDisabled.Remove(Name);
                }

                OnEnabled(Modbase.StaticHarmony);
            }
            else
            {
                // this can happen if an item is disabled 
                if (!PerformancePatches.onDisabled.ContainsKey(Name))
                {
                    PerformancePatches.onDisabled.Add(Name, () => OnDisabled(Modbase.StaticHarmony));
                }
            }
        }

        public virtual void OnEnabled(Harmony harmony)
        {
        }

        // The Disabled execution will not be immediate. It will be called when the window is closed, this is to prevent users spamming change and lagging if the intent is unpatching
        public virtual void OnDisabled(Harmony harmony)
        {
            isPatched = false; // probably :-)
        }

        public virtual void ExposeData()
        {
            // Prevent inability to access settings when prototyping
            var saveId = Name + "-isEnabled".Replace(" ", "-");
            Scribe_Values.Look(ref EnabledRefAccess(), saveId);
        }
    }
}