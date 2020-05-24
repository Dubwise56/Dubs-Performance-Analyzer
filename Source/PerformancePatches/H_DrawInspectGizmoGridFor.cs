using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DubsAnalyzer
{

    [PerformancePatch]
    [ProfileMode("DrawInspectGizmoGrid", UpdateMode.GUI)]
    internal class H_DrawInspectGizmoGridFor
    {
        public static bool Active = false;

        [Setting("Detour Mode", "GizmoInspectTip")]
        public static bool DetourMode = false;

        public static void PerformancePatch(Harmony harmony)
        {
            var pre = new HarmonyMethod(typeof(H_DrawInspectGizmoGridFor), nameof(Prefix));
            var post = new HarmonyMethod(typeof(H_DrawInspectGizmoGridFor), nameof(Postfix));
            var skiff = AccessTools.Method(typeof(InspectGizmoGrid), nameof(InspectGizmoGrid.DrawInspectGizmoGridFor));
            harmony.Patch(skiff, pre, post);

            harmony.Patch(AccessTools.Method(typeof(GizmoGridDrawer), nameof(GizmoGridDrawer.DrawGizmoGrid)), new HarmonyMethod(typeof(H_DrawInspectGizmoGridFor), nameof(Cacher)));
        }

        public static IEnumerable<Gizmo> cach;
        public static bool Cacher(IEnumerable<Gizmo> gizmos)
        {
            // nullcheck for edge cases
            if (gizmos == null) return false;

            if (Analyzer.Settings.OptimizeDrawInspectGizmoGrid)
            {
                cach = gizmos.ToList();
            }

            return true;
        }

        public static readonly string str = "InspectGizmoGrid.DrawInspectGizmoGridFor";
        public static bool Prefix(IEnumerable<object> selectedObjects, ref Gizmo mouseoverGizmo)
        {
            if (Active && DetourMode)
            {
                Detour(selectedObjects, ref mouseoverGizmo);
                return false;
            }


            if (!Active && !Analyzer.Settings.OptimizeDrawInspectGizmoGrid)
            {
                return true;
            }

            if (Active) Analyzer.Start(str);

            if (Analyzer.Settings.OptimizeDrawInspectGizmoGrid)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    return true;
                }

                if (Event.current.type != EventType.Layout)
                {
                    GizmoGridDrawer.DrawGizmoGrid(cach, InspectPaneUtility.PaneWidthFor(Find.WindowStack.WindowOfType<IInspectPane>()) + 20f, out mouseoverGizmo);
                }

                return false;
            }
            return true;
        }

        public static void Postfix()
        {
            if (Active) Analyzer.Stop(str);
        }

        public static void Detour(IEnumerable<object> selectedObjects, ref Gizmo mouseoverGizmo)
        {
            var DoRebuild = !(Analyzer.Settings.OptimizeDrawInspectGizmoGrid && Event.current.type != EventType.Repaint);

          

            if (DoRebuild)
            {
                mouseoverGizmo = null;
                try
                {
                    InspectGizmoGrid.objList.Clear();
                    InspectGizmoGrid.objList.AddRange(selectedObjects);
                    InspectGizmoGrid.gizmoList.Clear();
                    var slam = InspectGizmoGrid.objList.Count;
                    for (var i = 0; i < slam; i++)
                    {
                        if (InspectGizmoGrid.objList[i] is ISelectable selectable)
                        {
                            if (Active)
                            {
                                var me = string.Intern($"{selectable.GetType()} Gizmos");
                                Analyzer.Start(me);
                                InspectGizmoGrid.gizmoList.AddRange(selectable.GetGizmos());
                                Analyzer.Stop(me);
                            }
                            else
                            {
                                InspectGizmoGrid.gizmoList.AddRange(selectable.GetGizmos());
                            }

                        }
                    }
                    for (var j = 0; j < InspectGizmoGrid.objList.Count; j++)
                    {
                        if (InspectGizmoGrid.objList[j] is Thing t)
                        {
                            var allDesignators = Find.ReverseDesignatorDatabase.AllDesignators;
                            var coo = allDesignators.Count;
                            for (var k = 0; k < coo; k++)
                            {
                                Designator des = allDesignators[k];
                                if (des.CanDesignateThing(t).Accepted)
                                {
                                    var command_Action = new Command_Action
                                    {
                                        defaultLabel = des.LabelCapReverseDesignating(t),
                                        icon = des.IconReverseDesignating(t, out var iconAngle, out var iconOffset),
                                        iconAngle = iconAngle,
                                        iconOffset = iconOffset,
                                        defaultDesc = des.DescReverseDesignating(t),
                                        order = (!(des is Designator_Uninstall) ? -20f : -11f),
                                        action = delegate
                                        {
                                            if (!TutorSystem.AllowAction(des.TutorTagDesignate))
                                            {
                                                return;
                                            }

                                            des.DesignateThing(t);
                                            des.Finalize(true);
                                        },
                                        hotKey = des.hotKey,
                                        groupKey = des.groupKey
                                    };
                                    InspectGizmoGrid.gizmoList.Add(command_Action);
                                }
                            }
                        }
                    }
                    InspectGizmoGrid.objList.Clear();
                }
                catch (Exception ex)
                {
                    Log.ErrorOnce(ex.ToString(), 3427734);
                }
            }

            if (Active) Analyzer.Start(str);
            GizmoGridDrawer.DrawGizmoGrid(InspectGizmoGrid.gizmoList, InspectPaneUtility.PaneWidthFor(Find.WindowStack.WindowOfType<IInspectPane>()) + 20f, out mouseoverGizmo);
            if (Active) Analyzer.Stop(str);
        }

        //public static void Postfix()
        //{
        //    if (!Analyzer.running || Analyzer.UpdateMode != UpdateMode.GUI)
        //    {
        //        return;
        //    }

        //    if (Analyzer.loggingMode != LoggingMode.Windows)
        //    {
        //        return;
        //    }

        //    Analyzer.Stop("InspectGizmoGrid.DrawInspectGizmoGridFor");
        //}
    }
}