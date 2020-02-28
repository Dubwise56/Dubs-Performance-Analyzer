using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace DubsAnalyzer
{
    internal class MainButtonWorker_ToggleAnalyzer : MainButtonWorker
    {


        public override bool Disabled
        {
            get
            {
                this.def.buttonVisible = Analyzer.Settings.ShowOnMainTab;

                return Find.CurrentMap == null && (!def.validWithoutMap || def == MainButtonDefOf.World) ||
                       Find.WorldRoutePlanner.Active && Find.WorldRoutePlanner.FormingCaravan &&
                       (!def.validWithoutMap || def == MainButtonDefOf.World);
            }
        }

        public override void Activate()
        {
            try
            {
                if (Find.WindowStack.WindowOfType<Dialog_Analyzer>() != null)
                {
                    Find.WindowStack.RemoveWindowsOfType(typeof(Dialog_Analyzer));
                }
                else
                {
                    Find.WindowStack.Add(new Dialog_Analyzer());
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }
    }
}