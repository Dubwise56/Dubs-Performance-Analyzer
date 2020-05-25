using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace DubsAnalyzer
{
    public static class Dialog_StackedGraph
    {
        // Information we need;
        // Total Time taken by Tab
        // Top ~8? things contributing to this time
        // Time points for these 8 things
        public static void Display(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, Color.blue);

        }
    }
}
