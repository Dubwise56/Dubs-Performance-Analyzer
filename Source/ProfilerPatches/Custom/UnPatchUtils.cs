using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Verse;

namespace DubsAnalyzer
{
    public static class UnPatchUtils
    {
        private static Thread unpatchThread = null;
        private static bool currentlyPatching = false;

        public static void UnpatchMethod(string name)
        {
            if (currentlyPatching)
            {
                Messages.Message("Currently patching, please wait", MessageTypeDefOf.NegativeEvent, false);
                return;
            }
            unpatchThread = new Thread(() => UnpatchMethodFull(name));
            currentlyPatching = true;
            unpatchThread.Start();
        }
        private static void UnpatchMethodFull(string name)
        {
            var meth = AccessTools.Method(name);
            if(meth == null)
            {
                Messages.Message("Failed to locate method: " + name, MessageTypeDefOf.NegativeEvent, false);
                return;
            }

            foreach (var methodBase in Harmony.GetAllPatchedMethods())
            {
                var infos = Harmony.GetPatchInfo(methodBase);
                foreach (var infosPrefix in infos.Prefixes)
                {
                    if (infosPrefix.PatchMethod == meth)
                    {
                        Analyzer.harmony.Unpatch(methodBase, meth);
                    }
                }
                foreach (var infosPostfixesx in infos.Postfixes)
                {
                    if (infosPostfixesx.PatchMethod == meth)
                    {
                        Analyzer.harmony.Unpatch(methodBase, meth);
                    }
                }
            }

            currentlyPatching = false;
        }

        public static void UnpatchMethodsOnMethod(string name)
        {
            if (currentlyPatching)
            {
                Messages.Message("Currently patching, please wait", MessageTypeDefOf.NegativeEvent, false);
                return;
            }
            unpatchThread = new Thread(() => UnpatchMethodsOnMethodFull(name));
            currentlyPatching = true;
            unpatchThread.Start();
        }
        private static void UnpatchMethodsOnMethodFull(string name)
        {
            var meth = AccessTools.Method(name);
            if (meth == null)
            {
                Messages.Message("Failed to locate method: " + name, MessageTypeDefOf.NegativeEvent, false);
                return;
            }

            foreach (var methodBase in Harmony.GetAllPatchedMethods())
            {
                if (!(methodBase == meth))
                    continue;

                var infos = Harmony.GetPatchInfo(methodBase);

                Analyzer.harmony.Unpatch(methodBase, HarmonyPatchType.All, "*");
            }

            currentlyPatching = false;
        }
    }
}
