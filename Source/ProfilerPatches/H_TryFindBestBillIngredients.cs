using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DubsAnalyzer
{
    //internal class H_TryFindBestBillIngredients
    //{
    //    public static void PatchMe()
    //    {
    //        var skiff = AccessTools.Method(typeof(WorkGiver_DoBill), nameof(WorkGiver_DoBill.TryFindBestBillIngredients));
    //       // Analyzer.harmony.Patch(skiff, new HarmonyMethod(typeof(H_TryFindBestBillIngredients), nameof(Prefix)));
    //    }

    //    public static bool Prefix(WorkGiver_DoBill __instance, Bill bill, Pawn pawn, Thing billGiver, List<ThingCount> chosen, ref bool __result)
    //    {
    //        if (!Analyzer.Settings.ReplaceIngredientFinder)
    //        {
    //            return true;
    //        }
    //        WorkGiver_DoBill.relevantThings.Clear();
    //        WorkGiver_DoBill.processedThings.Clear();
    //        foreach (var recipeIngredient in bill.recipe.ingredients)
    //        {
    //            foreach (var filterAllowedDef in recipeIngredient.filter.allowedDefs)
    //            {
    //                foreach (var thing in pawn.Map.listerThings.listsByDef[filterAllowedDef])
    //                {
    //                    bool boo = (float)(thing.Position - billGiver.Position).LengthHorizontalSquared < bill.ingredientSearchRadius * bill.ingredientSearchRadius;
    //                    if (boo)
    //                    {
    //                        WorkGiver_DoBill.relevantThings.Add(thing);
    //                    }
    //                }
    //            }
    //        }
    //        if (WorkGiver_DoBill.TryFindBestBillIngredientsInSet(WorkGiver_DoBill.relevantThings, bill, chosen))
    //        {
    //            return true;
    //        }
    //        WorkGiver_DoBill.relevantThings.Clear();
    //        WorkGiver_DoBill.newRelevantThings.Clear();
    //        WorkGiver_DoBill.processedThings.Clear();
    //        WorkGiver_DoBill.ingredientsOrdered.Clear();
    //        return false;
    //    }
    //}
}