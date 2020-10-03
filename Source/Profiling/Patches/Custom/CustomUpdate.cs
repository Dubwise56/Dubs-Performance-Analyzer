using HarmonyLib;
using System.Reflection;

namespace Analyzer.Profiling
{
    [Entry("entry.update.custom", Category.Update, "entry.update.custom.tooltip")]
    internal class CustomProfilersUpdate
    {
        public static bool Active = false;
    }

}
