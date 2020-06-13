using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace DubsAnalyzer
{
    [ProfileMode("Game Component", UpdateMode.GUI)]
    public static class H_GameComponentUpdateGUI
    {
        public static bool Active = false;

        public static void ProfilePatch()
        {
            Analyzer.harmony.Patch(AccessTools.Method(typeof(GameComponentUtility), nameof(GameComponentUtility.GameComponentOnGUI)), new HarmonyMethod(typeof(H_GameComponentUpdateGUI), nameof(GameComponentTick)));
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
                    var prof = Analyzer.Start(trash, null, components[i].GetType(), null, null, __originalMethod);
                    components[i].GameComponentOnGUI();
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
