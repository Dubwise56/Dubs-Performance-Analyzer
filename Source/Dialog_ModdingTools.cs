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
    public enum CurrentInput
    {
        Method, Type, MethodHarmony, TypeHarmony //, Assembly
    }

    [StaticConstructorOnStartup]
    public static class Dialog_ModdingTools
    {
        // Custom patch a method, 
        // Type
        public static CurrentInput input = CurrentInput.Method;
        public static UpdateMode patchType = UpdateMode.Update;

        public static string currentInput = null;

        public static void DoWindowContents(Rect rect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect.ContractedBy(10f));

            listing.Label("CustoMethProfPatch".Translate());

            DisplayInputTypes(listing);


            if (input == CurrentInput.Method)
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
            } else // If we are not specifying a method, we will force use of Update
            {
                patchType = UpdateMode.Update;
            }

            string FieldDescription = null;
            
            switch(input)
            {
                case CurrentInput.Method: FieldDescription = "Type:Method"; break;
                case CurrentInput.Type: FieldDescription = "Type"; break;
                case CurrentInput.MethodHarmony: FieldDescription = "Type:Method"; break;
                case CurrentInput.TypeHarmony: FieldDescription = "Type"; break;
                    //case CurrentInput.Assembly: FieldDescription = "AssemblyName"; break;
            }

            Rect inputBox = listing.GetRect(25f);
            DubGUI.InputField(inputBox, FieldDescription, ref currentInput, ShowName: true);

            Rect patchBox = listing.GetRect(25f);
            if (Widgets.ButtonText(patchBox.LeftPartPixels(100), "TryCustoPatch".Translate()))
            {
                if (currentInput != null)
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
            }


            //listing.GapLine();

            //var b = listing.GetRect(25);
            //if (Widgets.ButtonText(b.LeftPartPixels(100), "TryCustoPatch".Translate()))
            //{
            //    if (customPatchMode == UpdateMode.Tick)
            //    {
            //        CustomProfilersTick.PatchMeth(methToPatch);
            //    }
            //    else
            //    {
            //        CustomProfilersUpdate.PatchMeth(methToPatch);
            //    }
            //}
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
    }
}
