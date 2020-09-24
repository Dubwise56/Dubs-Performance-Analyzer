using Analyzer.Profiling;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Analyzer.Performance
{
    public enum PerformanceCategory
    {
        Optimisation,
        ReplacesFunctionality,
        RemovesFunctionality
    }

    public class PerfPatch
    {
        public virtual string Name => "";
        public virtual PerformanceCategory category => PerformanceCategory.Optimisation;
        public AccessTools.FieldRef<bool> EnabledRefAccess;
        public bool isPatched = false;

        public void Initialise(Type subType)
        {
            EnabledRefAccess = AccessTools.StaticFieldRefAccess<bool>(subType.GetField("Enabled", BindingFlags.Public | BindingFlags.Static));
            if (EnabledRefAccess == null)
            {
                Log.Error("Add an 'Enabled' field you bloody muppet");
            }
        }

        public virtual void Draw(Listing_Standard listing)
        {
            var name = Name.TranslateSimple();
            var tooltip = (Name + ".tooltip").TranslateSimple();

            var height = Mathf.CeilToInt(name.GetWidthCached() / listing.ColumnWidth) * Text.LineHeight;
            var rect = listing.GetRect(height);

            if (DubGUI.Checkbox(rect, name, ref EnabledRefAccess()))
            {
                if (EnabledRefAccess())
                {
                    if (PerformancePatches.ondisabled.ContainsKey(Name))
                    {
                        PerformancePatches.ondisabled.Remove(Name);
                    }
                    OnEnabled(Modbase.StaticHarmony);
                }
                else
                {
                    PerformancePatches.ondisabled.Add(Name, () => OnDisabled(Modbase.StaticHarmony));
                }

            }
            TooltipHandler.TipRegion(rect, tooltip);
        }

        public virtual void OnEnabled(Harmony harmony) { }

        // The Disabled execution will not be immediate. It will be called when the window is closed, this is to prevent users spamming change and lagging if the intent is unpatching
        public virtual void OnDisabled(Harmony harmony)
        {
            isPatched = false; // probably :-)
        }

        public virtual void ExposeData()
        {
            string saveId = Name + "-isEnabled";
            Scribe_Values.Look(ref EnabledRefAccess(), saveId);
        }
    }
}
