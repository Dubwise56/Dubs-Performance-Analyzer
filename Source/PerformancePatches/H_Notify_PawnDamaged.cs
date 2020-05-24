using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace DubsAnalyzer
{
    [HarmonyPatch(typeof(Lord), nameof(Lord.Notify_PawnDamaged))]
    internal static class H_Notify_PawnDamaged
    {
        public static bool Prefix(Pawn victim)
        {
            if (Analyzer.Settings.NeverCheckJobsOnDamage)
            {
                var L = victim.GetLord().CurLordToil;
                if (L is LordToil_AssaultColony || L is LordToil_AssaultColonySappers)
                {
                    return false;
                }
            }
            return true;
        }
    }
}