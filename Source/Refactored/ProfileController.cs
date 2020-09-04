using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Analyzer
{
    public static class ProfileController
    {
        public static Dictionary<string, Profiler> profiles = new Dictionary<string, Profiler>();

        private static bool midUpdate = false;
        private static float deltaTime = 0.0f;

        public static Profiler Start(string key, Func<string> GetLabel = null, Type type = null, Def def = null, Thing thing = null, MethodBase meth = null)
        {
            if (!Analyzer.CurrentlyProfling) return null;

            if (profiles.TryGetValue(key, out Profiler prof)) return prof.Start();
            else
            {
                if (GetLabel != null) profiles[key] = new Profiler(key, GetLabel(), type, def, thing, meth);
                else profiles[key] = new Profiler(key, key, type, def, thing, meth);

                return profiles[key].Start();
            }
        }
        public static void Stop(string key)
        {
            if (profiles.TryGetValue(key, out Profiler prof))
                prof.Stop();
        }

        // Mostly here for book keeping, should be optimised out of a release build.
        [Conditional("DEBUG")]
        public static void BeginUpdate()
        {
#if DEBUG
            if (!Analyzer.CurrentlyProfling) return;

            if (midUpdate) Log.Error("[Analyzer] Attempting to begin new update cycle when the previous update has not ended");
            midUpdate = true;
#endif
        }

        public static void EndUpdate()
        {
            Analyzer.UpdateCycle(); // Update all our profilers, record measurements

            deltaTime += Time.deltaTime;
            if (deltaTime >= 1f)
            {
                Analyzer.FinishUpdateCycle(); // Process the information for all our profilers.
                deltaTime -= 1f;
            }
#if DEBUG
            midUpdate = false;
#endif
        }
    }
}
