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
        public static void Display(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, Color.blue);

            TabStats stats = new TabStats();
            stats.GenerateStats();
        }
    }
}
