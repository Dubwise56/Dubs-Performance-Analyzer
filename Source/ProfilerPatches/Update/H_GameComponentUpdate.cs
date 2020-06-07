using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace DubsAnalyzer
{


    [ProfileMode("Game Component", UpdateMode.Update)]
    public static class H_GameComponentUpdate
    {
        public static bool Active = false;

        public static void ProfilePatch()
        {
            Analyzer.harmony.Patch(AccessTools.Method(typeof(GameComponentUtility), nameof(GameComponentUtility.GameComponentUpdate)), new HarmonyMethod(typeof(H_GameComponentUpdate), nameof(GameComponentTick)));
        }

        public static bool GameComponentTick(MethodBase __originalMethod)
        {
            if (!Active) return true;

            List<GameComponent> components = Current.Game.components;
            for (int i = 0; i < components.Count; i++)
            {
                try
                {
                    var trash = components[i].GetType().Name;
                    Analyzer.Start(trash, null, components[i].GetType(), null, null, __originalMethod as MethodInfo);
                    components[i].GameComponentUpdate();
                    Analyzer.Stop(trash);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString(), false);
                }
            }
            return false;
        }
    }


}