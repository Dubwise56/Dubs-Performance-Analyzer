using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    public enum CurrentInput
    {
        Method,
        MethodHarmony,
        InternalMethod,
        Type,
        SubClasses,
        TypeHarmony,
        Assembly
    }

    internal class Panel_DevOptions
    {
        public static CurrentInput input = CurrentInput.Method;
        public static Category patchType = Category.Update;
        public static string currentInput = string.Empty;
        public static bool showSearchbox;
        public static void Draw(Listing_Standard listing, Rect win)
        {
            listing.Label(Strings.settings_dnspy);
            Settings.PathToDnspy = listing.TextEntry(Settings.PathToDnspy);
            listing.Gap();
            DubGUI.LabeledSliderFloat(listing, Strings.settings_updates_per_second, ref Settings.updatesPerSecond, 1.0f, 20.0f);
            DubGUI.Checkbox(Strings.settings_logging, listing, ref Settings.verboseLogging);
            DubGUI.Checkbox(Strings.settings_disable_tps_counter, listing, ref Settings.disableTPSCounter);
            DubGUI.Checkbox("settings.debuglog".Tr(), listing, ref Settings.enableLog);

            var s = Strings.settings_disable_cleanup;
            var rect = listing.GetRect(Text.LineHeight);
            DubGUI.Checkbox(rect, s, ref Settings.disableCleanup);
            TooltipHandler.TipRegion(rect, Strings.settings_disable_cleanup_desc);

            listing.GapLine();

            DubGUI.CenterText(() => listing.Label("devoptions.heading".Tr()));
            listing.GapLine();

            if (listing.ButtonTextLabeled("Logging cycle", patchType.ToString()))
            {
                if (patchType == Category.Tick)
                {
                    patchType = Category.Update;
                } 
                else
                {
                    patchType = Category.Tick;
                }
                //For if onGui gets added
                //var list = new List<FloatMenuOption>
                //{
                //    new FloatMenuOption("devoptions.patchtype.tick".Tr(), () => patchType = Category.Tick),
                //    new FloatMenuOption("devoptions.patchtype.update".Tr(), () => patchType = Category.Update)
                //    new FloatMenuOption("devoptions.patchtype.ongui".Tr(), () => patchType = Category.OnGui)
                //};
                //Find.WindowStack.Add(new FloatMenu(list));
            }

            if (showSearchbox)
            {
                Window_SearchBar.Control();
            }
            var inputR = DisplayInputField(listing);

            Window_SearchBar.SetCurrentInput(input);
            Window_SearchBar.UpdateSearchString(currentInput);

            var searchRect = listing.GetRect(Mathf.Min(listing.curY, win.height - listing.curY));

            lock (Window_SearchBar.sync)
            {
                if (showSearchbox && !Mouse.IsOver(searchRect) && Event.current.type != EventType.MouseDown)
                {
                    showSearchbox = false;
                }
                if (GUI.GetNameOfFocusedControl() == "profileinput")
                {
                    showSearchbox = true;
                }
                else
                if (Mouse.IsOver(inputR))
                {
                    showSearchbox = true;
                }
            }

            if (showSearchbox)
            {
                Window_SearchBar.DoWindowContents(searchRect);
            }
        }

        public static Rect DisplayInputField(Listing_Standard listing)
        {
            string FieldDescription = null;

            switch (input)
            {
                case CurrentInput.Method:
                    FieldDescription = "Type:Method";
                    break;
                case CurrentInput.Type:
                    FieldDescription = "Type";
                    break;
                case CurrentInput.MethodHarmony:
                    FieldDescription = "Type:Method";
                    break;
                case CurrentInput.TypeHarmony:
                    FieldDescription = "Type";
                    break;
                case CurrentInput.InternalMethod:
                    FieldDescription = "Type:Method";
                    break;
                case CurrentInput.SubClasses:
                    FieldDescription = "Type";
                    break;
                case CurrentInput.Assembly:
                    FieldDescription = "Mod or PackageId";
                    break;
            }

            var descWidth = FieldDescription.GetWidthCached() + 20f;
            var rect = listing.GetRect(Text.LineHeight + 8);
            var modeButt = rect.LeftPartPixels(descWidth);
            var patchButt = rect.RightPartPixels(50f);
            var inputRect = rect;
            inputRect.width -= modeButt.width + patchButt.width;
            inputRect.x = modeButt.xMax;

            if (Widgets.ButtonText(modeButt, FieldDescription))
            {
                var list = new List<FloatMenuOption>
                {
                    new FloatMenuOption(Strings.devoptions_input_method, () => input = CurrentInput.Method),
                    new FloatMenuOption(Strings.devoptions_input_methodinternal, () => input = CurrentInput.InternalMethod),
                    new FloatMenuOption(Strings.devoptions_input_methodharmony, () => input = CurrentInput.MethodHarmony),
                    new FloatMenuOption(Strings.devoptions_input_type, () => input = CurrentInput.Type),
                    new FloatMenuOption(Strings.devoptions_input_subclasses, () => input = CurrentInput.SubClasses),
                    new FloatMenuOption(Strings.devoptions_input_typeharmony, () => input = CurrentInput.TypeHarmony),
                    new FloatMenuOption(Strings.devoptions_input_assembly, () => input = CurrentInput.Assembly)
                };
                Find.WindowStack.Add(new FloatMenu(list));
            }

            DubGUI.InputField(inputRect, "profileinput", ref currentInput);

            if (Widgets.ButtonText(patchButt, "Patch"))
            {
                if (!string.IsNullOrEmpty(currentInput)) ExecutePatch();
            }

            return inputRect;
        }

        public static void ExecutePatch()
        {
            try
            {
                if (patchType == Category.Tick)
                {
                    switch (input)
                    {
                        case CurrentInput.Method:
                            MethodTransplanting.UpdateMethods(typeof(CustomProfilersTick),
                                Utility.GetMethods(currentInput));
                            break;
                        case CurrentInput.Type:
                            MethodTransplanting.UpdateMethods(typeof(CustomProfilersTick),
                                Utility.GetTypeMethods(AccessTools.TypeByName(currentInput)));
                            break;
                        case CurrentInput.MethodHarmony:
                            MethodTransplanting.UpdateMethods(typeof(CustomProfilersTick),
                                Utility.GetMethodsPatching(currentInput));
                            break;
                        case CurrentInput.SubClasses:
                            MethodTransplanting.UpdateMethods(typeof(CustomProfilersTick),
                                Utility.SubClassImplementationsOf(AccessTools.TypeByName(currentInput), m => true));
                            break;
                        case CurrentInput.TypeHarmony:
                            MethodTransplanting.UpdateMethods(typeof(CustomProfilersTick),
                                Utility.GetMethodsPatchingType(AccessTools.TypeByName(currentInput)));
                            break;
                        case CurrentInput.InternalMethod:
                            Utility.PatchInternalMethod(currentInput, Category.Tick);
                            return;
                        case CurrentInput.Assembly:
                            Utility.PatchAssembly(currentInput, Category.Tick);
                            return;
                    }

                    GUIController.Tab(Category.Tick).collapsed = false;
                    GUIController.SwapToEntry("Custom Tick");
                }
                else
                {
                    switch (input)
                    {
                        case CurrentInput.Method:
                            MethodTransplanting.UpdateMethods(typeof(CustomProfilersUpdate),
                                Utility.GetMethods(currentInput));
                            break;
                        case CurrentInput.Type:
                            MethodTransplanting.UpdateMethods(typeof(CustomProfilersUpdate),
                                Utility.GetTypeMethods(AccessTools.TypeByName(currentInput)));
                            break;
                        case CurrentInput.MethodHarmony:
                            MethodTransplanting.UpdateMethods(typeof(CustomProfilersUpdate),
                                Utility.GetMethodsPatching(currentInput));
                            break;
                        case CurrentInput.SubClasses:
                            MethodTransplanting.UpdateMethods(typeof(CustomProfilersUpdate),
                                Utility.SubClassImplementationsOf(AccessTools.TypeByName(currentInput), m => true));
                            break;
                        case CurrentInput.TypeHarmony:
                            MethodTransplanting.UpdateMethods(typeof(CustomProfilersUpdate),
                                Utility.GetMethodsPatchingType(AccessTools.TypeByName(currentInput)));
                            break;
                        case CurrentInput.InternalMethod:
                            Utility.PatchInternalMethod(currentInput, Category.Update);
                            return;
                        case CurrentInput.Assembly:
                            Utility.PatchAssembly(currentInput, Category.Update);
                            return;
                    }
                    GUIController.Tab(Category.Update).collapsed = false;
                    GUIController.SwapToEntry("Custom Update");
                }
            }
            catch (Exception e)
            {
                ThreadSafeLogger.Error($"Failed to process input, failed with the error {e.Message}");
            }
        }
    }
}