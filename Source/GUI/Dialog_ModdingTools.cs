using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using Verse;


namespace DubsAnalyzer
{
    public enum CurrentInput { Method, Type, MethodHarmony, TypeHarmony }
    public enum UnPatchType { Method, MethodsOnMethod, All }
    public enum WIPType { Assembly, InternalMethod }


    public static class Dialog_ModdingTools
    {
        public static CurrentInput input = CurrentInput.Method;
        public static UnPatchType unPatchType = UnPatchType.Method;
        public static UpdateMode patchType = UpdateMode.Update;
        public static WIPType wipType = WIPType.Assembly;

        public static string currentInput = null;
        public static string currentUnPatch = null;
        public static string currentWIP = null;
        public static float height = 500;
        public static Vector2 scrollPos = Vector2.zero;

        public static void DoWindowContents(Rect rect)
        {
            if (Event.current.type == EventType.Layout) return;

            Listing_Standard listing = new Listing_Standard();
            var view = rect.AtZero();

            Widgets.BeginScrollView(rect, ref scrollPos, view);
            GUI.BeginGroup(view);
            view.height = 9999;
            listing.Begin(view.ContractedBy(10f));

            DubGUI.Heading(listing, "Patch Methods");

            DisplayInputTypes(listing);

            if (input == CurrentInput.Method)
                DisplayPatchTypes(listing);
            else // If we are not patching a method, we default to 'update'
                patchType = UpdateMode.Update;
            
            DisplayInputField(listing);
            DisplayPatchButton(listing);

            listing.GapLine(12f);

            DubGUI.Heading(listing, "Unpatch Methods");

            DisplayUnPatchTypes(listing);
            DisplayUnPatchInputField(listing);
            DisplayUnPatchButton(listing);

            DubGUI.Heading(listing, "WIP Functionality");

            DisplayWIPTypes(listing);
            DisplayWIPField(listing);
            DisplayWIPButton(listing);

            listing.End();
            GUI.EndGroup();
            Widgets.EndScrollView();
        }

        public static void DisplayInputTypes(Listing_Standard listing)
        {
            Rect r = listing.GetRect(25f);
            if (Widgets.RadioButtonLabeled(r, "inputMethod".Translate(), input == CurrentInput.Method))
            {
                input = CurrentInput.Method;
            }
            r = listing.GetRect(25f);
            if (Widgets.RadioButtonLabeled(r, "inputType".Translate(), input == CurrentInput.Type))
            {
                input = CurrentInput.Type;
            }
            r = listing.GetRect(25f);
            if (Widgets.RadioButtonLabeled(r, "inputMethodHarmony".Translate(), input == CurrentInput.MethodHarmony))
            {
                input = CurrentInput.MethodHarmony;
            }
            r = listing.GetRect(25f);
            if (Widgets.RadioButtonLabeled(r, "inputTypeHarmony".Translate(), input == CurrentInput.TypeHarmony))
            {
                input = CurrentInput.TypeHarmony;
            }
            //r = listing.GetRect(25f);
            //if (Widgets.RadioButtonLabeled(r, "Assembly Patching (WIP)", input == CurrentInput.Assembly))
            //{
            //    input = CurrentInput.Assembly;
            //}
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
            Rect r = listing.GetRect(25f);
            if (Widgets.RadioButtonLabeled(r, "CustoUnPatchMeth".Translate(), unPatchType == UnPatchType.Method))
            {
                unPatchType = UnPatchType.Method;
            }
            r = listing.GetRect(25f);
            if (Widgets.RadioButtonLabeled(r, "CustoUnPatchAllOnMeth".Translate(), unPatchType == UnPatchType.MethodsOnMethod))
            {
                unPatchType = UnPatchType.MethodsOnMethod;
            }
            r = listing.GetRect(25f);
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

        public static void DisplayWIPTypes(Listing_Standard listing)
        {
            Rect r = listing.GetRect(25f);
            if (Widgets.RadioButtonLabeled(r, "Assembly Patching", wipType == WIPType.Assembly))
            {
                wipType = WIPType.Assembly;
            }
            r = listing.GetRect(25f);
            if (Widgets.RadioButtonLabeled(r, "Internal Method Patching", wipType == WIPType.InternalMethod))
            {
                wipType = WIPType.InternalMethod;
            }
        }
        public static void DisplayWIPButton(Listing_Standard listing)
        {
            Rect patchBox = listing.GetRect(25f);
            if (Widgets.ButtonText(patchBox.LeftPartPixels(100), "TryCustoPatch".Translate()))
            {
                if (currentWIP != null)
                {
                    ExecuteWIPPatch();
                }
            }
        }
        public static void DisplayWIPField(Listing_Standard listing)
        {
            string FieldDescription = null;

            switch (wipType)
            {
                case WIPType.Assembly: FieldDescription = "AssemblyName or PackageID"; break;
                case WIPType.InternalMethod: FieldDescription = "Type:Method"; break;
            }

            Rect inputBox = listing.GetRect(25f);
            DubGUI.InputField(inputBox, FieldDescription, ref currentWIP, ShowName: true);
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
                //case CurrentInput.Assembly:
                //    CustomProfilersUpdate.PatchAssembly(currentInput);
                //    break;
            }
        }
        public static void ExecuteUnPatch()
        {
            switch (unPatchType)
            {
                case UnPatchType.Method:            PatchUtils.UnpatchMethod(currentUnPatch);   break;
                case UnPatchType.MethodsOnMethod:   PatchUtils.UnpatchMethod(currentUnPatch);   break;
                case UnPatchType.All:               Analyzer.unPatchMethods(true);              break;
            }
        }
        public static void ExecuteWIPPatch()
        {
            switch (wipType)
            {
                case WIPType.Assembly:              CustomProfilersUpdate.PatchAssembly(currentWIP); break;
                case WIPType.InternalMethod:        PatchUtils.PatchInternalMethod(currentWIP); break;
            }
        }
    }

}
