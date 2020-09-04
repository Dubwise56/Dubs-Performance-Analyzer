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
    [Entry("HarmonyTranspilers", UpdateMode.Update, "TransPatchTipKey")]
    internal class H_HarmonyTranspilers
    {
        public static bool Active = false;

        public static string TipCache = string.Empty;
        public static string LogCache;

        public static void MouseOver(Rect r, Profiler prof, ProfileLog log)
        {
            if (LogCache != log.Label)
            {
                LogCache = log.Label;
                TipCache = string.Empty;
                //   var patches = Harmony.GetAllPatchedMethods().ToList();

                //    foreach (var methodBase in patches)
                //    {
                Patches infos = Harmony.GetPatchInfo(log.Meth);
                //foreach (var infosPrefix in infos.Prefixes)
                //{
                //    if (infosPrefix.PatchMethod == log.Meth)
                //    {
                //        TipCache += $"{infosPrefix.owner} {infosPrefix.PatchMethod}\n";
                //    }
                //}
                //foreach (var infosPostfixesx in infos.Postfixes)
                //{
                //    if (infosPostfixesx.PatchMethod == log.Meth)
                //    {
                //        TipCache += $"{infosPostfixesx.owner} {infosPostfixesx.PatchMethod}\n";
                //    }
                //}
                foreach (Patch infosPostfixesx in infos.Transpilers)
                {
                    //  if (infosPostfixesx.PatchMethod == log.Meth)
                    //  {
                    TipCache += $"{infosPostfixesx.owner} - {infosPostfixesx.PatchMethod.Name}\n\n";
                    // }
                }
                // }
            }
            TooltipHandler.TipRegion(r, TipCache);
        }

        public static void Clicked(Profiler prof, ProfileLog log)
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (log.Meth != null)
                {
                    Modbase.harmony.Unpatch(log.Meth, HarmonyPatchType.Transpiler, "*");
                    Messages.Message("Unpatched", MessageTypeDefOf.TaskCompletion, false);
                }
                else
                {
                    Messages.Message("Null method", MessageTypeDefOf.NegativeEvent, false);
                }

            }
        }

        public static List<MethodBase> PatchedMeths = new List<MethodBase>();
        public static void ProfilePatch()
        {
            HarmonyMethod go = new HarmonyMethod(typeof(H_HarmonyTranspilers), nameof(Prefix));
            HarmonyMethod biff = new HarmonyMethod(typeof(H_HarmonyTranspilers), nameof(Postfix));
            HarmonyMethod trans = new HarmonyMethod(typeof(H_HarmonyTranspilers), nameof(Transpiler));

            int c = 0;
            int p = 0;
            List<MethodBase> patches = Harmony.GetAllPatchedMethods().ToList();
            foreach (MethodBase mode in patches)
            {
                try
                {
                    c++;

                    Patches patchInfo = Harmony.GetPatchInfo(mode);
                    System.Collections.ObjectModel.ReadOnlyCollection<Patch> pilers = patchInfo.Transpilers;
                    if (!pilers.NullOrEmpty())
                    {
                        p++;

                        if (pilers.Any(x => x.owner != Modbase.harmony.Id && x.owner != Modbase.perfharmony.Id) && !PatchedMeths.Contains(mode))
                        {
                            PatchedMeths.Add(mode);
                            Modbase.harmony.Patch(mode, go, biff);
                        }
                    }
                }
                catch (Exception)
                {

                }

            }
        }

        /* The idea here is a little convoluted, but in short
         * 1. Grab the 'original' instructions of the method, and grab all the 'Call'/'CallVirt' instructions
         * 2. Grab all the 'Call'/'CallVirt' instructions in the modified version
         */

        public static IEnumerable<CodeInstruction> Transpiler(MethodBase __originalMethod, IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
        {
            List<CodeInstruction> inst = PatchProcessor.GetOriginalInstructions(__originalMethod);
            List<CodeInstruction> modInstList = instructions.ToList();

            List<CodeInstruction> origCalls = inst.Where(i => InternalMethodUtility.IsFunctionCall(i.opcode)).ToList();
            List<CodeInstruction> modCalls = modInstList.Where(i => InternalMethodUtility.IsFunctionCall(i.opcode)).ToList();

            return instructions;
        }

        [HarmonyPriority(Priority.Last)]
        public static void Prefix(MethodBase __originalMethod, ref Profiler __state)
        {
            if (!Active) return;

            __state = Modbase.Start(__originalMethod.Name, () =>
            {
                if (__originalMethod.ReflectedType != null)
                {
                    return $"{__originalMethod.Name} - {__originalMethod.ReflectedType.FullName}";
                }
                return $"{__originalMethod.Name} - {__originalMethod.GetType().FullName}";
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

    [Entry("HarmonyPatches", UpdateMode.Update, "HarmPatchesTipKey")]
    internal class H_HarmonyPatches
    {
        public static bool Active = false;
        public static void Clicked(Profiler prof, ProfileLog log)
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (log.Meth == null)
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
                        if (infosPrefix.PatchMethod == log.Meth)
                        {
                            Modbase.harmony.Unpatch(methodBase, HarmonyPatchType.All, "*");
                            Messages.Message("Unpatched prefixes", MessageTypeDefOf.TaskCompletion, false);
                        }
                    }
                    foreach (Patch infosPostfixesx in infos.Postfixes)
                    {
                        if (infosPostfixesx.PatchMethod == log.Meth)
                        {
                            Modbase.harmony.Unpatch(methodBase, HarmonyPatchType.All, "*");
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
                        if (Modbase.harmony.Id != fix.owner && Modbase.perfharmony.Id != fix.owner && !PatchedPres.Contains(fix))
                        {
                            PatchedPres.Add(fix);
                            Modbase.harmony.Patch(fix.PatchMethod, go, biff);
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
                        if (Modbase.harmony.Id != fix.owner && Modbase.perfharmony.Id != fix.owner && !PatchedPosts.Contains(fix))
                        {
                            PatchedPosts.Add(fix);
                            Modbase.harmony.Patch(fix.PatchMethod, go, biff);
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

            __state = Modbase.Start(__originalMethod.GetHashCode().ToString(), () =>
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