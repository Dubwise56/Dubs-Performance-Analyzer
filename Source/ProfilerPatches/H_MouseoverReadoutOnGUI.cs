using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace DubsAnalyzer
{
    //[HarmonyPatch(typeof(MouseoverReadout), nameof(MouseoverReadout.MouseoverReadoutOnGUI))]
    //internal class H_MouseoverReadoutOnGUI
    //{
    //    private static bool Prefix(MouseoverReadout __instance)
    //    {
    //        if (Event.current.type != EventType.Repaint)
    //        {
    //            return false;
    //        }

    //        if (Find.MainTabsRoot.OpenTab != null)
    //        {
    //            return false;
    //        }

    //        GenUI.DrawTextWinterShadow(new Rect(256f, UI.screenHeight - 256, -256f, 256f));
    //        Text.Font = GameFont.Small;
    //        GUI.color = new Color(1f, 1f, 1f, 0.8f);
    //        var c = UI.MouseCell();
    //        if (!c.InBounds(Find.CurrentMap))
    //        {
    //            return false;
    //        }

    //        var num = 0f;
    //        Rect rect;
    //        if (c.Fogged(Find.CurrentMap))
    //        {
    //            rect = new Rect(MouseoverReadout.BotLeft.x, UI.screenHeight - MouseoverReadout.BotLeft.y - num, 999f,
    //                999f);
    //            Widgets.Label(rect, "Undiscovered".Translate());
    //            GUI.color = Color.white;
    //            return false;
    //        }

    //        rect = new Rect(MouseoverReadout.BotLeft.x, UI.screenHeight - MouseoverReadout.BotLeft.y - num, 999f, 999f);
    //        var num2 = Mathf.RoundToInt(Find.CurrentMap.glowGrid.GameGlowAt(c) * 100f);
    //        Widgets.Label(rect, __instance.glowStrings[num2]);
    //        num += 19f;
    //        rect = new Rect(MouseoverReadout.BotLeft.x, UI.screenHeight - MouseoverReadout.BotLeft.y - num, 999f, 999f);
    //        var terrain = c.GetTerrain(Find.CurrentMap);
    //        if (terrain != __instance.cachedTerrain)
    //        {
    //            var str = (double)terrain.fertility <= 0.0001? string.Empty: string.Intern($" {"FertShort".Translate()} {terrain.fertility.ToStringPercent()}");

    //            __instance.cachedTerrainString = "";// terrain.LabelCap + (terrain.passability == Traversability.Impassable? null: string.Intern($" ({"WalkSpeed".Translate(__instance.SpeedPercentString(terrain.pathCost))}{str})"));
    //            __instance.cachedTerrain = terrain;
    //        }

    //        Widgets.Label(rect, __instance.cachedTerrainString);
    //        num += 19f;
    //        var zone = c.GetZone(Find.CurrentMap);
    //        if (zone != null)
    //        {
    //            rect = new Rect(MouseoverReadout.BotLeft.x, UI.screenHeight - MouseoverReadout.BotLeft.y - num, 999f,
    //                999f);
    //            var label = zone.label;
    //            Widgets.Label(rect, label);
    //            num += 19f;
    //        }

    //        var depth = Find.CurrentMap.snowGrid.GetDepth(c);
    //        if (depth > 0.03f)
    //        {
    //            rect = new Rect(MouseoverReadout.BotLeft.x, UI.screenHeight - MouseoverReadout.BotLeft.y - num, 999f,
    //                999f);
    //            var snowCategory = SnowUtility.GetSnowCategory(depth);

               
    //            var dam = SnowUtility.GetDescription(snowCategory);
    //            var dab = SnowUtility.MovementTicksAddOn(snowCategory);
    //            var jam =  SpeedPercentString(dab);

    //            var label2 = "";// string.Intern($"{dam} ({"WalkSpeed".Translate(jam)})");
    //            Widgets.Label(rect, label2);
    //            num += 19f;
    //        }

    //        var thingList = c.GetThingList(Find.CurrentMap);
    //        for (var i = 0; i < thingList.Count; i++)
    //        {
    //            var thing = thingList[i];
    //            if (thing.def.category != ThingCategory.Mote)
    //            {
    //                rect = new Rect(MouseoverReadout.BotLeft.x, UI.screenHeight - MouseoverReadout.BotLeft.y - num,
    //                    999f, 999f);
    //                var labelMouseover = string.Intern(thing.LabelMouseover);
    //                Widgets.Label(rect, labelMouseover);
    //                num += 19f;
    //            }
    //        }

    //        var roof = c.GetRoof(Find.CurrentMap);
    //        if (roof != null)
    //        {
    //            rect = new Rect(MouseoverReadout.BotLeft.x, UI.screenHeight - MouseoverReadout.BotLeft.y - num, 999f,
    //                999f);
    //            Widgets.Label(rect,roof.LabelCap);
    //            num += 19f;
    //        }

    //        GUI.color = Color.white;

    //        return false;
    //    }

    //    private static string SpeedPercentString(float extraPathTicks)
    //    {
    //        float f = 13f / (extraPathTicks + 13f);
    //        return f.ToStringPercent();
    //    }
    //}
}