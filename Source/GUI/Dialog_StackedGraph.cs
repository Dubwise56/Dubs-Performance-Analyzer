using UnityEngine;

namespace Analyzer
{
    public static class Dialog_StackedGraph
    {
        public static void Display(Rect rect)
        {
            //Widgets.DrawBoxSolid(rect, Color.blue);

            TabStats stats = new TabStats();
            stats.GenerateStats();
        }
    }
}
