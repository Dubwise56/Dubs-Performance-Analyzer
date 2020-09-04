using RimWorld;
using System;
using Verse;

namespace Analyzer
{
    internal class MainButton_Toggle : MainButtonWorker
    {
        public override bool Disabled
        {
            get
            {
                return Find.CurrentMap == null
                    && (!def.validWithoutMap || def == MainButtonDefOf.World) || Find.WorldRoutePlanner.Active
                    && Find.WorldRoutePlanner.FormingCaravan
                    && (!def.validWithoutMap || def == MainButtonDefOf.World);
            }
        }

        public override void Activate()
        {
            if (Find.WindowStack.WindowOfType<Window_Analyzer>() != null) 
            { 
                Find.WindowStack.RemoveWindowsOfType(typeof(Window_Analyzer));
            }
            else 
            { 
                Find.WindowStack.Add(new Window_Analyzer());
            }
        }
    }
}