using RimWorld;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Analyzer
{
    internal class MainButton_Toggle : MainButtonWorker
    {
        public override bool Disabled
        {
            get
            {
                this.def.buttonVisible = Settings.showIcon;

                return Find.CurrentMap == null
                    && (!def.validWithoutMap || def == MainButtonDefOf.World) || Find.WorldRoutePlanner.Active
                    && Find.WorldRoutePlanner.FormingCaravan
                    && (!def.validWithoutMap || def == MainButtonDefOf.World);
            }
        }

        public override void Activate()
        {
            if (Event.current.button == 0)
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
            else
            {
                var options = new List<FloatMenuOption>();

                if (Find.WindowStack.WindowOfType<Window_Analyzer>() == null && Modbase.isPatched)
                {
                    options.Add(new FloatMenuOption("Cleanup", () => Current.Game.GetComponent<GameComponent_Analyzer>().TimeTillCleanup = 0));
                }
#if V1_7
                options.Add(new FloatMenuOption(Strings.frameprofiler_button, Profiling.VanillaProfilerMirror.ToggleWindow));
#endif

                if (options.Count > 0)
                    Find.WindowStack.Add(new FloatMenu(options));
            }
        }
    }
}