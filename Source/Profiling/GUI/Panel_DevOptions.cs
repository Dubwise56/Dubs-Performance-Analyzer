	using System;
using System.Collections.Generic;
using System.Linq;
	using System.Reflection;
	using HarmonyLib;
	using RimWorld;
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

		public static void Draw(Listing_Standard listing, Rect win, bool settingsPage)
		{
			listing.Label(Strings.settings_dnspy);
			Settings.PathToDnspy = listing.TextEntry(Settings.PathToDnspy);
			listing.Gap();
			DubGUI.LabeledSliderFloat(listing, Strings.settings_updates_per_second, ref Settings.updatesPerSecond, 1.0f, 20.0f);
			DubGUI.Checkbox(Strings.settings_logging, listing, ref Settings.verboseLogging);
			DubGUI.Checkbox(Strings.settings_disable_tps_counter, listing, ref Settings.disableTPSCounter);
			DubGUI.Checkbox(Strings.settings_enable_debug_log, listing, ref Settings.enableLog);
			DubGUI.Checkbox(Strings.settings_show_icon, listing, ref Settings.showIcon);
			if(DubGUI.Checkbox(Strings.settings_long_form_names, listing, ref Settings.longFormNames)) {
				ThreadSafeLogger.Warning("You will need to restart analyzer to see changes come into effect for tabs which are already open. (Right click on the analyzer icon, and click 'Cleanup' ; or wait 30s after closing analyzer)");
			}
			DubGUI.Checkbox(Strings.settings_disable_threading, listing, ref Settings.disableThreadedPatching);

			var s = Strings.settings_disable_cleanup;
			var rect = listing.GetRect(Text.LineHeight);
			DubGUI.Checkbox(rect, s, ref Settings.disableCleanup);
			TooltipHandler.TipRegion(rect, Strings.settings_disable_cleanup_desc);

			if (settingsPage) return;

			listing.GapLine();

			DubGUI.CenterText(() => listing.Label("devoptions.heading".Tr()));
			listing.GapLine();

			var tabs = listing.GetRect(tabRect.height);
			tabs.width = tabRect.width;

			Drawtab(tabs, 0, "Patch Tools");
			tabs.x = tabs.xMax;
			Drawtab(tabs, 1, $"Saved Patches ({Settings.SavedPatches_Tick.Count + Settings.SavedPatches_Update.Count})");
			listing.Gap(4);
			if (PatchTab == 0)
			{
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
			else
			{
				foreach (var patch in Settings.SavedPatches_Tick.ToList())
				{
					var row = listing.GetRect(Text.LineHeight);
					if (Widgets.CloseButtonFor(row.LeftPartPixels(30f)))
					{
						Settings.SavedPatches_Tick.Remove(patch);
					}
					Widgets.Label(row.RightPartPixels(row.width - 30f), patch + " Tick");
					listing.GapLine();
				}
				foreach (var patch in Settings.SavedPatches_Update.ToList())
				{
					var row = listing.GetRect(Text.LineHeight);
					if (Widgets.CloseButtonFor(row.LeftPartPixels(30f)))
					{
						Settings.SavedPatches_Update.Remove(patch);
					}
					Widgets.Label(row.RightPartPixels(row.width - 30f), patch + " Update");
					listing.GapLine();
				}
			}
		}

		private static int PatchTab = 0;
		static Rect tabRect = new Rect(0, 0, 150, 18);
		public static void Drawtab(Rect r, int i, string lab)
		{
			r.height += 1;
			r.width += 1;
			Widgets.DrawMenuSection(r);
			if (PatchTab == i)
			{
				var hang = r.ContractedBy(1f);
				hang.y += 2;
				Widgets.DrawBoxSolid(hang, Widgets.MenuSectionBGFillColor);
			}

			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Tiny;
			Widgets.Label(r, lab);
			DubGUI.ResetFont();
			if (Widgets.ButtonInvisible(r))
			{
				PatchTab = i;
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
			var patchButt = rect.RightPartPixels(100f);
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

			if (input == CurrentInput.Method)
			{
				if (Widgets.ButtonText(patchButt.LeftHalf(), "Patch"))
				{
					if (!string.IsNullOrEmpty(currentInput)) ExecutePatch(input, currentInput, patchType);
				}
				if (Widgets.ButtonText(patchButt.RightHalf(), "Save"))
				{
					if (!string.IsNullOrEmpty(currentInput))
					{
						if (patchType == Category.Tick)
						{
							Settings.SavedPatches_Tick.Add(currentInput);
						}
						else
						{
							Settings.SavedPatches_Update.Add(currentInput);
						}
						Modbase.Settings.Write();
						ExecutePatch(input, currentInput, patchType);
					}
				}
			}
			else
			{
				if (Widgets.ButtonText(patchButt, "Patch"))
				{
					if (!string.IsNullOrEmpty(currentInput)) ExecutePatch(input, currentInput, patchType);
				}
			}


			return inputRect;
		}

		public static void ExecutePatch(CurrentInput mode, string strinput, Category cat)
		{
			try
			{

				var entry = cat == Category.Tick ? typeof(CustomProfilersTick) : typeof(CustomProfilersUpdate);
				List<MethodInfo> methods = null;
				
				var temp = mode switch
				{
					CurrentInput.Method => Utility.GetMethods(strinput),
					CurrentInput.Type => Utility.GetTypeMethods(AccessTools.TypeByName(strinput)),
					CurrentInput.MethodHarmony => Utility.GetMethodsPatching(strinput),
					CurrentInput.SubClasses => Utility.SubClassImplementationsOf(AccessTools.TypeByName(strinput), m => true),
					CurrentInput.TypeHarmony => Utility.GetMethodsPatchingType(AccessTools.TypeByName(strinput)),
					_ => null,
				};

				if (temp is null)
				{
					if(mode == CurrentInput.InternalMethod)
						Utility.PatchInternalMethod(strinput, cat);
					else
						Utility.PatchAssembly(strinput, cat);

					return;
				}
				
				methods = temp.ToList();

				if (methods.Count == 0)
				{
					Messages.Message($"Failed to find the method(s) represented by {strinput}", MessageTypeDefOf.CautionInput, false);
					return;
				}
				
				Messages.Message(methods.Count != 1 ? $"Patching {methods.Count} methods in the category {cat}" : $"Patching {Utility.GetSignature(methods[0])} in the category {cat}", MessageTypeDefOf.CautionInput, false);
				
				MethodTransplanting.UpdateMethods(entry, methods.ToList());
				GUIController.Tab(cat).collapsed = false;

				var entryName = (cat == Category.Tick) ? "Custom Tick" : "Custom Update";
				GUIController.SwapToEntry(entryName);
			}
			catch (Exception e)
			{
				ThreadSafeLogger.ReportException(e, $"Failed to process search bar input");
			}
		}
	}
}