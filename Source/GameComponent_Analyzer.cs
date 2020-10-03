using Analyzer.Performance;
using Analyzer.Profiling;
using Analyzer.Fixes;
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
        private Game game = null;

        public GameComponent_Analyzer(Game game)
        {
            this.game = game;
            // On game load, initialise the currently active performance patches
            PerformancePatches.ActivateEnabledPatches();

            FixPatches.OnGameInit(game);
        }

        public override void LoadedGame()
        {
            FixPatches.OnGameLoad(game);
        }


        public override void GameComponentUpdate()
        {
            // Display our logged messages that we may have recieved from other threads.
            ThreadSafeLogger.DisplayLogs();
        }

    }
}