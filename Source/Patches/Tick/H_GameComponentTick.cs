using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Analyzer
{
    [Entry("GameComponent", UpdateMode.Tick)]
    public static class H_GameComponent
    {
        public static bool Active = false;

        public static void ProfilePatch()
        {
            Modbase.harmony.Patch(AccessTools.Method(typeof(GameComponentUtility), nameof(GameComponentUtility.GameComponentTick)), new HarmonyMethod(typeof(H_GameComponent), nameof(GameComponentTick)));
        }

        public static bool GameComponentTick(MethodBase __originalMethod)
        {
            if (!Active) return true;

            List<GameComponent> components = Current.Game.components;
            for (int i = 0; i < components.Count; i++)
            {
                try
                {
                    string trash = components[i].GetType().Name;
                    Profiler prof = Modbase.Start(trash, null, components[i].GetType(), null, null, __originalMethod);
                    components[i].GameComponentTick();
                    prof.Stop();
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
