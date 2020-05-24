using HarmonyLib;
using Verse;

namespace DubsAnalyzer
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Tick))]
    internal static class H_PawnTick
    {
        public static bool TickPawns = true;

        public static bool Prefix()
        {
            if (!TickPawns)
            {
                return false;
            }

            return true;
        }
    }
}