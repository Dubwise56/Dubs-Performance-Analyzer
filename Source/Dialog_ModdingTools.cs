using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;


namespace DubsAnalyzer
{
    public enum CurrentInput { Method, Type, MethodHarmony, TypeHarmony /*, Assembly */ }
    public enum UnPatchType { Method, MethodsOnMethod, All }

    [StaticConstructorOnStartup]
    public static class Dialog_ModdingTools
    {
        // Custom patch a method, 
        // Type
        public static CurrentInput input = CurrentInput.Method;
        public static UnPatchType unPatchType = UnPatchType.Method;
        public static UpdateMode patchType = UpdateMode.Update;


        public static string currentInput = null;
        public static string currentUnPatch = null;

        public static void DoWindowContents(Rect rect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect.ContractedBy(10f));

            Heading(listing, "Patch Methods");

            DisplayInputTypes(listing);

            if (input == CurrentInput.Method)
                DisplayPatchTypes(listing);
            else // If we are not patching a method, we default to 'update'
                patchType = UpdateMode.Update;
            

            DisplayInputField(listing);
            DisplayPatchButton(listing);

            listing.GapLine(12f);

            Heading(listing, "Unpatch Methods");

            DisplayUnPatchTypes(listing);
            DisplayUnPatchInputField(listing);
            DisplayUnPatchButton(listing);
        }

        public static void Heading(Listing_Standard listing, string label)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(listing.GetRect(30), label);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public static void DisplayInputTypes(Listing_Standard listing)
        {
            Rect r = listing.GetRect(25f).LeftPartPixels(450);
            if (Widgets.RadioButtonLabeled(r, "inputMethod".Translate(), input == CurrentInput.Method))
            {
                input = CurrentInput.Method;
            }
            r = listing.GetRect(25f).LeftPartPixels(450);
            if (Widgets.RadioButtonLabeled(r, "inputType".Translate(), input == CurrentInput.Type))
            {
                input = CurrentInput.Type;
            }
            r = listing.GetRect(25f).LeftPartPixels(450);
            if (Widgets.RadioButtonLabeled(r, "inputMethodHarmony".Translate(), input == CurrentInput.MethodHarmony))
            {
                input = CurrentInput.MethodHarmony;
            }
            r = listing.GetRect(25f).LeftPartPixels(450);
            if (Widgets.RadioButtonLabeled(r, "inputTypeHarmony".Translate(), input == CurrentInput.TypeHarmony))
            {
                input = CurrentInput.TypeHarmony;
            }
        }
        public static void DisplayPatchTypes(Listing_Standard listing)
        {
            Rect r = listing.GetRect(25f).LeftPartPixels(150);
            if (Widgets.RadioButtonLabeled(r, "CustoTickPatch".Translate(), patchType == UpdateMode.Tick))
            {
                patchType = UpdateMode.Tick;
            }
            r = listing.GetRect(25f).LeftPartPixels(150);
            if (Widgets.RadioButtonLabeled(r, "CustoUpdatePatch".Translate(), patchType == UpdateMode.Update))
            {
                patchType = UpdateMode.Update;
            }
        }
        public static void DisplayInputField(Listing_Standard listing)
        {
            string FieldDescription = null;

            switch (input)
            {
                case CurrentInput.Method: FieldDescription = "Type:Method"; break;
                case CurrentInput.Type: FieldDescription = "Type"; break;
                case CurrentInput.MethodHarmony: FieldDescription = "Type:Method"; break;
                case CurrentInput.TypeHarmony: FieldDescription = "Type"; break;
                    //case CurrentInput.Assembly: FieldDescription = "AssemblyName"; break;
            }

            Rect inputBox = listing.GetRect(25f);
            DubGUI.InputField(inputBox, FieldDescription, ref currentInput, ShowName: true);
        }
        public static void DisplayPatchButton(Listing_Standard listing)
        {
            Rect patchBox = listing.GetRect(25f);
            if (Widgets.ButtonText(patchBox.LeftPartPixels(100), "TryCustoPatch".Translate()))
            {
                if (currentInput != null)
                {
                    ExecutePatch();
                }
            }
        }

        public static void DisplayUnPatchTypes(Listing_Standard listing)
        {
            // Method, MethodsOnMethod, All
            Rect r = listing.GetRect(25f).LeftPartPixels(450);
            if (Widgets.RadioButtonLabeled(r, "CustoUnPatchMeth".Translate(), unPatchType == UnPatchType.Method))
            {
                unPatchType = UnPatchType.Method;
            }
            r = listing.GetRect(25f).LeftPartPixels(450);
            if (Widgets.RadioButtonLabeled(r, "CustoUnPatchAllOnMeth".Translate(), unPatchType == UnPatchType.MethodsOnMethod))
            {
                unPatchType = UnPatchType.Method;
            }
            r = listing.GetRect(25f).LeftPartPixels(450);
            if (Widgets.RadioButtonLabeled(r, "CustoUnPatchAll".Translate(), unPatchType == UnPatchType.All))
            {
                unPatchType = UnPatchType.All;
            }
        }
        public static void DisplayUnPatchButton(Listing_Standard listing)
        {
            Rect patchBox = listing.GetRect(25f);
            if (Widgets.ButtonText(patchBox.LeftPartPixels(100), "TryCustoUnPatch".Translate()))
            {
                if (currentInput != null || unPatchType == UnPatchType.All)
                {
                    ExecuteUnPatch();
                }
            }
        }
        public static void DisplayUnPatchInputField(Listing_Standard listing)
        {
            string FieldDescription = null;

            switch (unPatchType)
            {
                case UnPatchType.Method:            FieldDescription = "Type:Method";   break;
                case UnPatchType.MethodsOnMethod:   FieldDescription = "Type:Method";   break;
                case UnPatchType.All:               FieldDescription = "N/A";           break;
            }

            Rect inputBox = listing.GetRect(25f);
            DubGUI.InputField(inputBox, FieldDescription, ref currentUnPatch, ShowName: true);
        }
        public static void ExecutePatch()
        {
            switch (input)
            {
                case CurrentInput.Method:
                    if (patchType == UpdateMode.Tick)
                        CustomProfilersTick.PatchMeth(currentInput);
                    else
                        CustomProfilersUpdate.PatchMeth(currentInput);
                    break;
                case CurrentInput.Type:
                    CustomProfilersUpdate.PatchType(currentInput);
                    break;
                case CurrentInput.MethodHarmony:
                    CustomProfilersHarmony.PatchMeth(currentInput);
                    break;
                case CurrentInput.TypeHarmony:
                    CustomProfilersHarmony.PatchType(currentInput);
                    break;
            }
        }
        public static void ExecuteUnPatch()
        {
            switch (unPatchType)
            {
                case UnPatchType.Method:            UnPatchUtils.UnpatchMethod(currentUnPatch);   break;
                case UnPatchType.MethodsOnMethod:   UnPatchUtils.UnpatchMethod(currentUnPatch);   break;
                case UnPatchType.All:               Analyzer.unPatchMethods(true);              break;
            }
        }
    }

}
