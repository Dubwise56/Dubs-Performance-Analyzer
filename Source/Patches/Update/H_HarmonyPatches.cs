using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace Analyzer
{

    [Entry("HarmonyPatches", Category.Update, "HarmPatchesTipKey")]
    internal class H_HarmonyPatches
    {
        public static bool Active = false;
        public static void Clicked(Profiler prof, ProfileLog log)
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (log.meth == null)
                {
                    Messages.Message("Null method", MessageTypeDefOf.NegativeEvent, false);
                    return;
                }

                List<MethodBase> patches = Harmony.GetAllPatchedMethods().ToList();
                // Patches patchInfo = Harmony.GetPatchInfo(log.Meth);

                foreach (MethodBase methodBase in patches)
                {
                    Patches infos = Harmony.GetPatchInfo(methodBase);
                    foreach (Patch infosPrefix in infos.Prefixes)
                    {
                        if (infosPrefix.PatchMethod == log.meth)
                        {
                            Modbase.Harmony.Unpatch(methodBase, HarmonyPatchType.All, "*");
                            Messages.Message("Unpatched prefixes", MessageTypeDefOf.TaskCompletion, false);
                        }
                    }
                    foreach (Patch infosPostfixesx in infos.Postfixes)
                    {
                        if (infosPostfixesx.PatchMethod == log.meth)
                        {
                            Modbase.Harmony.Unpatch(methodBase, HarmonyPatchType.All, "*");
                            Messages.Message("Unpatched postfixes", MessageTypeDefOf.TaskCompletion, false);
                        }
                    }
                }
            }
        }

        public static List<Patch> PatchedPres = new List<Patch>();
        public static List<Patch> PatchedPosts = new List<Patch>();
        public static void ProfilePatch()
        {
            HarmonyMethod go = new HarmonyMethod(typeof(H_HarmonyPatches), nameof(Prefix));
            HarmonyMethod biff = new HarmonyMethod(typeof(H_HarmonyPatches), nameof(Postfix));
            List<MethodBase> patches = Harmony.GetAllPatchedMethods().ToList();

            foreach (MethodBase mode in patches)
            {
                Patches patchInfo = Harmony.GetPatchInfo(mode);
                foreach (Patch fix in patchInfo.Prefixes)
                {
                    try
                    {
                        if (Modbase.Harmony.Id != fix.owner && !PatchedPres.Contains(fix))
                        {
                            PatchedPres.Add(fix);
                            Modbase.Harmony.Patch(fix.PatchMethod, go, biff);
                        }
                    }
                    catch (Exception)
                    {

                    }
                }

                foreach (Patch fix in patchInfo.Postfixes)
                {
                    try
                    {
                        if (Modbase.Harmony.Id != fix.owner && !PatchedPosts.Contains(fix))
                        {
                            PatchedPosts.Add(fix);
                            Modbase.Harmony.Patch(fix.PatchMethod, go, biff);
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }

        [HarmonyPriority(Priority.Last)]
        public static void Prefix(MethodBase __originalMethod, ref Profiler __state)
        {
            if (!Active) return;

            __state = ProfileController.Start(__originalMethod.GetHashCode().ToString(), () =>
            {
                if (__originalMethod.ReflectedType != null)
                {
                    return $"{__originalMethod.ToString()} - {__originalMethod.ReflectedType.FullName}";
                }
                return $"{__originalMethod.ToString()} - {__originalMethod.GetType().FullName}";
            }, null, null, null, __originalMethod);
        }

        [HarmonyPriority(Priority.First)]
        public static void Postfix(Profiler __state)
        {
            if (Active)
            {
                __state?.Stop();
            }
        }
    }
}