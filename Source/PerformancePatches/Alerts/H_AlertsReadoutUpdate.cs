using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace DubsAnalyzer
{
    [PerformancePatch]
    [ProfileMode("Alerts", UpdateMode.Update, "AlertsTipKey",
        true)]
    internal class H_AlertsReadoutUpdate
    {
        public static bool Active = false;

        public static void Clicked(Profiler prof, ProfileLog log)
        {

        }

        public static void Checkbox(Profiler prof, ProfileLog log)
        {
            if (log.Type != null)
            {
                if (!Analyzer.Settings.AlertFilter.ContainsKey(log.Type))
                {
                    Analyzer.Settings.AlertFilter.Add(log.Type, true);
                }
                else
                {
                    var bam = Analyzer.Settings.AlertFilter[log.Type];
                    Analyzer.Settings.AlertFilter[log.Type] = !bam;
                }
            }
        }

        public static bool Selected(Profiler prof, ProfileLog log)
        {
            if (log.Type == null)
            {
                return false;
            }

            if (Analyzer.Settings.AlertFilter.ContainsKey(log.Type))
            {
                var bam = Analyzer.Settings.AlertFilter[log.Type];
                if (bam)
                {
                    return false;
                }
            }

            return true;
        }

        public static void PerformancePatch()
        {
            var biff = new HarmonyMethod(typeof(H_AlertsReadoutUpdate), nameof(CheckAddOrRemoveAlert));
            var skiff = AccessTools.Method(typeof(AlertsReadout), nameof(AlertsReadout.CheckAddOrRemoveAlert));
            Analyzer.harmony.Patch(skiff, biff);

            var skiff2 = AccessTools.Method(typeof(AlertsReadout), nameof(AlertsReadout.AlertsReadoutOnGUI));
            Analyzer.harmony.Patch(skiff2, new HarmonyMethod(typeof(H_AlertsReadoutUpdate), nameof(AlertsReadoutOnGUI)));

             //   skiff2 = AccessTools.Method(typeof(AlertsReadout), nameof(AlertsReadout.AlertsReadoutUpdate));
               // Analyzer.harmony.Patch(skiff2, new HarmonyMethod(typeof(H_AlertsReadoutUpdate), nameof(AlertsReadoutUpdate)));
        }



        public static bool CheckAddOrRemoveAlert(AlertsReadout __instance, Alert alert, bool forceRemove)
        {
        //    return false;
            if (!Analyzer.Settings.OverrideAlerts && (!Analyzer.running || !Active))
            {
                return true;
            }

            try
            {
                var typeis = alert.GetType();

                // if (Analyzer.loggingMode == LoggingMode.RenderingThings)
                //  {
                //name = $"{alert.GetType()}";
                //  }

                if (Active)
                {
                    Analyzer.Start(typeis.Name, () => typeis.FullName, typeis);
                }

                var ac = false;

                if (Analyzer.Settings.AlertFilter.ContainsKey(typeis))
                {
                    if (Analyzer.Settings.AlertFilter[typeis] == false)
                    {
                        alert.Recalculate();
                        ac = alert.Active;
                    }
                }
                else
                {
                    alert.Recalculate();
                    ac = alert.Active;
                }

                if (Active)
                {
                    Analyzer.Stop(typeis.Name);
                }

                if (!forceRemove && ac)
                {
                    if (!__instance.activeAlerts.Contains(alert))
                    {
                        __instance.activeAlerts.Add(alert);
                        alert.Notify_Started();
                    }
                }
                else
                {
                    __instance.activeAlerts.Remove(alert);
                }
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("Exception processing alert " + alert.ToString() + ": " + ex.ToString(), 743575, false);
                __instance.activeAlerts.Remove(alert);
            }

            return false;
        }

        public static bool AlertsReadoutOnGUI(AlertsReadout __instance)
        {
       //     return false;
            if (!Analyzer.Settings.OverrideAlerts && (!Analyzer.running || !Active))
            {
                return true;
            }

            if (!Active)
            {
                return true;
            }

            if (Event.current.type == EventType.Layout || Event.current.type == EventType.MouseDrag)
            {
                return false;
            }

            if (__instance.activeAlerts.Count == 0)
            {
                return false;
            }

            Alert alert = null;
            var alertPriority = AlertPriority.Critical;
            var flag = false;
            var num = Find.LetterStack.LastTopY - __instance.activeAlerts.Count * 28f;
            var rect = new Rect(UI.screenWidth - 154f, num, 154f, __instance.lastFinalY - num);
            var num2 = GenUI.BackgroundDarkAlphaForText();
            if (num2 > 0.001f)
            {
                GUI.color = new Color(1f, 1f, 1f, num2);
                Widgets.DrawShadowAround(rect);
                GUI.color = Color.white;
            }

            var num3 = num;
            if (num3 < 0f)
            {
                num3 = 0f;
            }

            for (var i = 0; i < __instance.PriosInDrawOrder.Count; i++)
            {
                var alertPriority2 = __instance.PriosInDrawOrder[i];
                for (var j = 0; j < __instance.activeAlerts.Count; j++)
                {
                    var alert2 = __instance.activeAlerts[j];
                    if (alert2.Priority == alertPriority2)
                    {
                        if (!flag)
                        {
                            alertPriority = alertPriority2;
                            flag = true;
                        }

                        var key = alert2.GetType();
                        Analyzer.Start(key.Name, () => key.FullName, key);
                        var rect2 = alert2.DrawAt(num3, alertPriority2 != alertPriority);
                        Analyzer.Stop(key.Name);

                        if (Mouse.IsOver(rect2))
                        {
                            alert = alert2;
                            __instance.mouseoverAlertIndex = j;
                        }

                        num3 += rect2.height;
                    }
                }
            }

            __instance.lastFinalY = num3;
            UIHighlighter.HighlightOpportunity(rect, "Alerts");
            if (alert != null)
            {
                alert.DrawInfoPane();
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Alerts, KnowledgeAmount.FrameDisplayed);
                __instance.CheckAddOrRemoveAlert(alert, false);
            }

            return false;
        }

        public static bool AlertsReadoutUpdate(AlertsReadout __instance)
        {
         //   return false;
            if (!Analyzer.Settings.OverrideAlerts && !Analyzer.running)
            {
                return true;
            }

            if (!Active)
            {
                return true;
            }

            if (Mathf.Max(Find.TickManager.TicksGame, Find.TutorialState.endTick) < 600)
            {
                return false;
            }
            if (Find.Storyteller.def.disableAlerts)
            {
                __instance.activeAlerts.Clear();
                return false;
            }
            __instance.curAlertIndex++;
            if (__instance.curAlertIndex >= 24)
            {
                __instance.curAlertIndex = 0;
            }
            for (int i = __instance.curAlertIndex; i < __instance.AllAlerts.Count; i += 24)
            {
                __instance.CheckAddOrRemoveAlert(__instance.AllAlerts[i], false);
            }
            if (Time.frameCount % 20 == 0)
            {
                List<Quest> questsListForReading = Find.QuestManager.QuestsListForReading;
                for (int j = 0; j < questsListForReading.Count; j++)
                {
                    List<QuestPart> partsListForReading = questsListForReading[j].PartsListForReading;
                    for (int k = 0; k < partsListForReading.Count; k++)
                    {
                        QuestPartActivable questPartActivable = partsListForReading[k] as QuestPartActivable;
                        Alert cachedAlert = questPartActivable?.CachedAlert;
                        if (cachedAlert != null)
                        {
                            bool flag = questsListForReading[j].State != QuestState.Ongoing || questPartActivable.State != QuestPartState.Enabled;
                            bool alertDirty = questPartActivable.AlertDirty;
                            __instance.CheckAddOrRemoveAlert(cachedAlert, flag || alertDirty);
                            if (alertDirty)
                            {
                                questPartActivable.ClearCachedAlert();
                            }
                        }
                    }
                }
            }

            for (int l = __instance.activeAlerts.Count - 1; l >= 0; l--)
            {
                Alert alert = __instance.activeAlerts[l];

                try
                {
                    var hash = __instance.activeAlerts[l].GetHashCode().ToString();
                    Analyzer.Start(hash, () => __instance.activeAlerts[l] +" Update", __instance.activeAlerts[l].GetType());
                    __instance.activeAlerts[l].AlertActiveUpdate();
                    Analyzer.Stop(hash);
                }
                catch (Exception ex)
                {
                    Log.ErrorOnce("Exception updating alert " + alert.ToString() + ": " + ex.ToString(), 743575, false);
                    __instance.activeAlerts.RemoveAt(l);
                }

            }

            if (__instance.mouseoverAlertIndex >= 0 && __instance.mouseoverAlertIndex < __instance.activeAlerts.Count)
            {
                IEnumerable<GlobalTargetInfo> allCulprits = __instance.activeAlerts[__instance.mouseoverAlertIndex].GetReport().AllCulprits;
                if (allCulprits != null)
                {
                    foreach (GlobalTargetInfo target in allCulprits)
                    {
                        TargetHighlighter.Highlight(target, true, true, false);
                    }
                }
            }
            __instance.mouseoverAlertIndex = -1;

            return false;
        }
    }
}