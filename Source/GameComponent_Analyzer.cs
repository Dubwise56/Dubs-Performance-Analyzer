using Analyzer.Performance;
using Analyzer.Profiling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Analyzer
{
    public class GameComponent_Analyzer : GameComponent
    {
        public GameComponent_Analyzer(Game game)
        {
            PerformancePatches.ActivateEnabledPatches();
        }

        public override void GameComponentUpdate()
        {
            // Display our logged messages that we may have recieved from other threads.
            ThreadSafeLogger.DisplayLogs();
        }
    }
}