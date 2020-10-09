using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    class Panel_StackTraces
    {
        public static bool currentlyTracking = false;
        public static int currentTrackedStacktraces = 0;
        public static int currentGoalTrackedTraces = 100_000;

        public static MethodInfo postfix = AccessTools.Method(typeof(Panel_StackTraces), nameof(StacktracePostfix));

        public static void Draw(Rect rect, GeneralInformation? info)
        {
            if (info == null || info.Value.method == null) return;
            var method = info.Value.method;

            if (DubGUI.Checkbox(rect.TopPartPixels(30f), "Enable Stacktrace Tracking for " + method.Name, ref currentlyTracking))
            {
                if(currentlyTracking) Modbase.Harmony.Patch(method, postfix: new HarmonyMethod(postfix));
                else Modbase.Harmony.CreateProcessor(method).Unpatch(postfix);

                StackTraceRegex.Reset();
            }

            rect.AdjustVerticallyBy(30f);

            if (Widgets.ButtonText(rect.TopPartPixels(30f), "Unpatch stack tracer"))
            {
                Modbase.Harmony.CreateProcessor(method).Unpatch(postfix);
            }

            rect.AdjustVerticallyBy(30f);

            var listing = new Listing_Standard();
            listing.Begin(rect);

            foreach (var st in StackTraceRegex.traces.OrderBy(w => w.Value.Count).Reverse())
            {
                StackTraceInformation traceInfo = st.Value;
                listing.Label("Total Calls: " + st.Value.Count);

                for (int i = 0; i < st.Value.TranslatedArr().Length - 1; i++)
                {
                    var r = listing.GetRect(Text.LineHeight);

                    Widgets.Label(r, st.Value.TranslatedArr()[i]);
                }


                break;
            }

            listing.End();
        }


        
        // This will be added as a Postfix to the method which we want to gather stack trace information for
        // it will only effect one method, so we can skip the check, and it will not slow down other profilers
        // because it will only be patched onto one method. There can be extra checks and flexibility in how
        // many frames are grabbed p/s etc. These are to be done when the GUI decisions have been made.

        public static void StacktracePostfix()
        {
            if (++currentTrackedStacktraces < currentGoalTrackedTraces) StackTraceRegex.Add(new StackTrace(2, false));
            else currentlyTracking = false;
        }

    }
}
