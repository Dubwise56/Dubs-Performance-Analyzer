using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Analyzer
{
    [StaticConstructorOnStartup]
    public class Gfx
    {
        public static Texture2D disco = ContentFinder<Texture2D>.Get("DPA/UI/discord", false);
        public static Texture2D Support = ContentFinder<Texture2D>.Get("DPA/UI/Support", false);
    }

    public class Settings : ModSettings
    {
        public static Listing_Standard listing = new Listing_Standard();

        public static string methToPatch = string.Empty;

        public static List<MethodInfo> GotMeth = new List<MethodInfo>();
        public Color LineCol = new Color32(79, 147, 191, 255);
        public Color GraphCol = new Color32(17, 17, 17, 255);
        public static string MethSearch = string.Empty;
        public static string TypeSearch = string.Empty;

        public string @PathToDnspy = "";
        public bool verboseLogging;
        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref LineCol, "LineCol", new Color32(79, 147, 191, 255));
            Scribe_Values.Look(ref GraphCol, "GraphCol", new Color32(17, 17, 17, 255));
            Scribe_Values.Look(ref verboseLogging, "verboseLogging", false);
        }

        private static Vector2 scrollPos;
        public static float settingsHeight = 500;

        public void DoSettings(Rect canvas)
        {
            if (Event.current.type == EventType.Layout) return;

            Rect view = canvas.AtZero();
            view.height = settingsHeight;
            Widgets.BeginScrollView(canvas, ref scrollPos, view, false);
            GUI.BeginGroup(view);
            view.height = 9999;
            listing.Begin(view.ContractedBy(10f));

            { // Draw the github and discord textures / Icons
                if (80 + "PerfAnalWiki".Translate().GetWidthCached() + "DubModDisco".Translate().GetWidthCached() < listing.ColumnWidth)
                {
                    Rect rec = listing.GetRect(24f);
                    Rect lrec = rec.LeftHalf();
                    rec = rec.RightHalf();
                    Widgets.DrawTextureFitted(lrec.LeftPartPixels(40f), Gfx.Support, 1f);
                    lrec.x += 40;
                    if (Widgets.ButtonText(lrec.LeftPartPixels("PerfAnalWiki".Translate().GetWidthCached()), "PerfAnalWiki".Translate(), false, true))
                    {
                        Application.OpenURL("https://github.com/Dubwise56/Dubs-Performance-Analyzer/wiki");
                    }

                    Widgets.DrawTextureFitted(rec.RightPartPixels(40f), Gfx.disco, 1f);
                    rec.width -= 40;
                    if (Widgets.ButtonText(rec.RightPartPixels("DubModDisco".Translate().GetWidthCached()), "DubModDisco".Translate(), false, true))
                    {
                        Application.OpenURL("https://discord.gg/Az5CnDW");
                    }
                }
                else
                {
                    Rect rec = listing.GetRect(24f);
                    Widgets.DrawTextureFitted(rec.LeftPartPixels(40f), Gfx.Support, 1f);
                    if (Widgets.ButtonText(rec.RightPartPixels(rec.width - 40), "PerfAnalWiki".Translate(), false, true))
                    {
                        Application.OpenURL("https://github.com/Dubwise56/Dubs-Performance-Analyzer/wiki");
                    }

                    rec = listing.GetRect(24f);
                    Widgets.DrawTextureFitted(rec.LeftPartPixels(40f), Gfx.disco, 1f);
                    if (Widgets.ButtonText(rec.RightPartPixels(rec.width - 40), "DubModDisco".Translate(), false, true))
                    {
                        Application.OpenURL("https://discord.gg/Az5CnDW");
                    }
                }
            }

            listing.GapLine();

            DrawDevOptions();

            listing.GapLine();

            settingsHeight = listing.GetRect(25).yMax;

            listing.End();
            GUI.EndGroup();
            Widgets.EndScrollView();
        }

        /* For Dev Tools */

        public void DrawDevOptions()
        {
            listing.Label("settings.developer.heading".Translate());
            DubGUI.InputField(listing.GetRect(Text.LineHeight), "Path to Dnspy.exe (including the exe)", ref PathToDnspy, ShowName: true);

            listing.GapLine();

            //Dialog_DeveloperSettings.DrawOptions(listing);
        }

    }
}

//public static class Dialog_DeveloperSettings
//    {
//        public enum UtilInput { SubTypes, ImplementedMethods }
//        public static UtilInput input = UtilInput.SubTypes;
//        public static string currentInput = null;
//        public static void DrawOptions(Listing_Standard listing)
//        {
//            Rect left = listing.GetRect(Text.LineHeight * 11); // the average height of this is ~226, which is 10.2 * Text.LineHeight
//            Rect right = left.RightPart(0.48f);
//            left = left.LeftPart(0.48f);

//            PatchOptions.DrawPatches(left);
//            PatchOptions.DrawUnPatches(right);

//            // maybe you want to get to this later.. doesn't work great

//            //DubGUI.CenterText(() => listing.Label("Utilites"));

//            //var rect = listing.GetRect((Text.LineHeight + 3) * 2);
//            //var rightRect = rect.RightPart(.48f);
//            //rect = rect.LeftPart(.48f);

//            //DubGUI.OptionalBox(rect.TopPart(0.48f), "input.subtypes".Translate(), () => input = UtilInput.SubTypes, input == UtilInput.SubTypes);
//            //DubGUI.OptionalBox(rect.BottomPart(0.48f), "input.methodimplementations".Translate(), () => input = UtilInput.ImplementedMethods, input == UtilInput.ImplementedMethods);
//            //DisplayUtilInputField(listing);
//            //if (Widgets.ButtonText(listing.GetRect(Text.CalcHeight("patch".Translate(), listing.ColumnWidth/2.0f)).LeftPart(.48f), "patch".Translate()))
//            //    if (currentInput != null)
//            //        ExecuteUtilPatch();

//        }

//        public static void DisplayUtilInputField(Listing_Standard listing)
//        {
//            string FieldDescription = null;

//            switch (input)
//            {
//                case UtilInput.SubTypes: FieldDescription = "Type"; break;
//                case UtilInput.ImplementedMethods: FieldDescription = "Type:Method"; break;
//            }

//            Rect inputBox = listing.GetRect(Text.LineHeight);
//            inputBox = inputBox.LeftPart(.48f);
//            DubGUI.InputField(inputBox, FieldDescription, ref currentInput, ShowName: true);
//        }

//        public static void ExecuteUtilPatch()
//        {
//            if (input == UtilInput.SubTypes)
//            {
//                foreach (Type subType in AccessTools.TypeByName(currentInput).AllSubclasses())
//                {
//                    CustomProfilersUpdate.PatchType(subType.FullName);
//                }
//                AnalyzerState.SwapTab("Custom Update", UpdateMode.Update);
//            }
//            else
//            {
//                foreach (Type subType in AccessTools.Method(currentInput).DeclaringType.AllSubclasses())
//                {
//                    foreach (MethodInfo method in subType.GetMethods().Where(m => m.Name == currentInput))
//                    {
//                        CustomProfilersUpdate.PatchMeth(method.DeclaringType.FullName + ":" + method.Name);
//                    }
//                }
//                AnalyzerState.SwapTab("Custom Update", UpdateMode.Update);
//            }
//        }

//        internal static class PatchOptions
//        {
//            public enum CurrentInput { Method, Type, MethodHarmony, TypeHarmony, InternalMethod, Assembly }
//            public enum UnPatchType { Method, MethodsOnMethod, Type, InternalMethod, All }

//            public static CurrentInput input = CurrentInput.Method;
//            public static UnPatchType unPatchType = UnPatchType.Method;
//            public static UpdateMode patchType = UpdateMode.Update;

//            public static string currentInput = null;
//            public static string currentUnPatch = null;
//            public static string currentWIP = null;

//            public static void DrawPatches(Rect left)
//            {
//                Listing_Standard lListing = new Listing_Standard();
//                lListing.Begin(left);

//                DubGUI.CenterText(() => lListing.Label("ProfilePatchMethod".Translate()));

//                lListing.GapLine(6);
//                DubGUI.OptionalBox(lListing.GetRect(Text.LineHeight + 3), "input.method".Translate(), () => input = CurrentInput.Method, input == CurrentInput.Method);
//                DubGUI.OptionalBox(lListing.GetRect(Text.LineHeight + 3), "input.type".Translate(), () => input = CurrentInput.Type, input == CurrentInput.Type);
//                DubGUI.OptionalBox(lListing.GetRect(Text.LineHeight + 3), "input.methodharmony".Translate(), () => input = CurrentInput.MethodHarmony, input == CurrentInput.MethodHarmony);
//                DubGUI.OptionalBox(lListing.GetRect(Text.LineHeight + 3), "input.typeharmony".Translate(), () => input = CurrentInput.TypeHarmony, input == CurrentInput.TypeHarmony);
//                DubGUI.OptionalBox(lListing.GetRect(Text.LineHeight + 3), "input.methodinternal".Translate(), () => input = CurrentInput.InternalMethod, input == CurrentInput.InternalMethod);
//                DubGUI.OptionalBox(lListing.GetRect(Text.LineHeight + 3), "input.assembly".Translate(), () => input = CurrentInput.Assembly, input == CurrentInput.Assembly);
//                lListing.curY += 2;

//                DisplayInputField(lListing);
//                lListing.curY += 2;

//                Rect box = lListing.GetRect(Text.LineHeight + 3);

//                DubGUI.OptionalBox(box.LeftPart(.3f), "patch.type.tick".Translate(), () => patchType = UpdateMode.Tick, patchType == UpdateMode.Tick);
//                box = box.RightPart(.65f);
//                DubGUI.OptionalBox(box.LeftPart(.4f), "patch.type.update".Translate(), () => patchType = UpdateMode.Update, patchType == UpdateMode.Update);

//                if (Widgets.ButtonText(box.RightPart(.5f), "patch".Translate()))
//                    if (currentInput != null)
//                        ExecutePatch();

//                lListing.End();
//            }

//            public static void DisplayInputField(Listing_Standard listing)
//            {
//                string FieldDescription = null;

//                switch (input)
//                {
//                    case CurrentInput.Method: FieldDescription = "Type:Method"; break;
//                    case CurrentInput.Type: FieldDescription = "Type"; break;
//                    case CurrentInput.MethodHarmony: FieldDescription = "Type:Method"; break;
//                    case CurrentInput.TypeHarmony: FieldDescription = "Type"; break;
//                    case CurrentInput.InternalMethod: FieldDescription = "Type:Method"; break;
//                    case CurrentInput.Assembly: FieldDescription = "Mod or PackageId"; break;
//                }

//                Rect inputBox = listing.GetRect(Text.LineHeight);
//                DubGUI.InputField(inputBox, FieldDescription, ref currentInput, ShowName: true);
//                //SearchBar.PopulateSearch(inputBox, currentInput, input);
//            }
//            public static void ExecutePatch()
//            {
//                switch (input)
//                {
//                    case CurrentInput.Method:
//                        if (patchType == UpdateMode.Tick)
//                        {
//                            CustomProfilersTick.PatchMeth(currentInput);
//                            AnalyzerState.SwapTab("Custom Tick", UpdateMode.Tick);
//                        }
//                        else
//                        {
//                            CustomProfilersUpdate.PatchMeth(currentInput);
//                            AnalyzerState.SwapTab("Custom Update", UpdateMode.Update);
//                        }
//                        return;
//                    case CurrentInput.Type:
//                        CustomProfilersUpdate.PatchType(currentInput);
//                        AnalyzerState.SwapTab("Custom Update", UpdateMode.Update);
//                        return;
//                    case CurrentInput.MethodHarmony:
//                        CustomProfilersHarmony.PatchMeth(currentInput);
//                        AnalyzerState.SwapTab("Custom Harmony", UpdateMode.Update);
//                        return;
//                    case CurrentInput.TypeHarmony:
//                        CustomProfilersHarmony.PatchType(currentInput);
//                        AnalyzerState.SwapTab("Custom Harmony", UpdateMode.Update);
//                        return;
//                    case CurrentInput.InternalMethod:
//                        PatchUtils.PatchInternalMethod(currentInput);
//                        return;
//                    case CurrentInput.Assembly:
//                        PatchUtils.PatchAssembly(currentInput, false);
//                        return;
//                }
//            }
//            public static void DrawUnPatches(Rect right)
//            {
//                Listing_Standard rListing = new Listing_Standard();
//                rListing.Begin(right);

//                DubGUI.CenterText(() => rListing.Label("UnProfilePatchMethod".Translate()));
//                rListing.GapLine(6);
//                DubGUI.OptionalBox(rListing.GetRect(Text.LineHeight + 3), "input.unpatchmethod".Translate(), () => unPatchType = UnPatchType.Method, unPatchType == UnPatchType.Method);
//                DubGUI.OptionalBox(rListing.GetRect(Text.LineHeight + 3), "input.unpatchmethodsonmethod".Translate(), () => unPatchType = UnPatchType.MethodsOnMethod, unPatchType == UnPatchType.MethodsOnMethod);
//                DubGUI.OptionalBox(rListing.GetRect(Text.LineHeight + 3), "input.unpatchtype".Translate(), () => unPatchType = UnPatchType.Type, unPatchType == UnPatchType.Type);
//                DubGUI.OptionalBox(rListing.GetRect(Text.LineHeight + 3), "input.unpatchinternalmethod".Translate(), () => unPatchType = UnPatchType.InternalMethod, unPatchType == UnPatchType.InternalMethod);
//                DubGUI.OptionalBox(rListing.GetRect(Text.LineHeight + 3), "input.unpatchall".Translate(), () => unPatchType = UnPatchType.All, unPatchType == UnPatchType.All);
//                rListing.curY += 2;

//                DisplayUnPatchInputField(rListing);
//                rListing.curY += 2;
//                if (Widgets.ButtonText(rListing.GetRect(Text.LineHeight + 3), "unpatch".Translate()))
//                    if (currentInput != null || unPatchType == UnPatchType.All)
//                        ExecuteUnPatch();

//                rListing.End();
//            }

//            public static void DisplayUnPatchInputField(Listing_Standard listing)
//            {
//                string FieldDescription = null;

//                switch (unPatchType)
//                {
//                    case UnPatchType.Method: FieldDescription = "Type:Method"; break;
//                    case UnPatchType.MethodsOnMethod: FieldDescription = "Type:Method"; break;
//                    case UnPatchType.Type: FieldDescription = "Type"; break;
//                    case UnPatchType.InternalMethod: FieldDescription = "Type:Method"; break;
//                    case UnPatchType.All: FieldDescription = "N/A"; break;
//                }

//                Rect inputBox = listing.GetRect(Text.LineHeight);
//                DubGUI.InputField(inputBox, FieldDescription, ref currentUnPatch, ShowName: true);
//            }

//            public static void ExecuteUnPatch()
//            {
//                switch (unPatchType)
//                {
//                    case UnPatchType.Method: PatchUtils.UnpatchMethod(currentUnPatch); break;
//                    case UnPatchType.MethodsOnMethod: PatchUtils.UnpatchMethod(currentUnPatch); break;
//                    case UnPatchType.Type: PatchUtils.UnPatchTypePatches(currentUnPatch); break;
//                    case UnPatchType.InternalMethod: PatchUtils.UnpatchInternalMethod(currentUnPatch); break;
//                    case UnPatchType.All: Modbase.UnPatchMethods(true); break;
//                }
//            }

//            internal static class SearchBar
//            {
//                public static Rect searchBoxRect;
//                public static Thread searchThread = null;
//                public static HashSet<string> cachedEntries = new HashSet<string>();
//                public static bool curSearching = false;
//                public static string prevInput = "";
//                public static object sync = new object();
//                private static float yHeigthCache = 9999999;
//                private static Vector2 searchpos = Vector2.zero;
//                public static Listing_Standard listing = new Listing_Standard();

//                public static void PopulateSearch(Rect rect, string searchText, CurrentInput inputType)
//                {
//                    bool active = false;
//                    lock (sync)
//                    {
//                        active = curSearching;
//                    }

//                    if (!active && prevInput != currentInput)
//                    {
//                        switch (inputType)
//                        {
//                            case CurrentInput.Method:
//                            case CurrentInput.InternalMethod:
//                            case CurrentInput.MethodHarmony:
//                                searchThread = new Thread(() => PopulateSearchMethod(searchText));
//                                break;
//                            case CurrentInput.Type:
//                            case CurrentInput.TypeHarmony:
//                                searchThread = new Thread(() => PopulateSearchType(searchText));
//                                break;
//                            default:
//                                searchThread = new Thread(() => PopulateSearchAssembly(searchText));
//                                break;

//                        }
//                        searchThread.IsBackground = true;
//                        prevInput = currentInput;
//                        searchThread.Start();
//                    }
//                    searchBoxRect = rect;
//                }

//                private static void PopulateSearchMethod(string searchText)
//                {
//                    if (searchText.Length <= 4) return;

//                    lock (sync)
//                    {
//                        curSearching = true;
//                    }

//                    HashSet<string> names = new HashSet<string>();

//                    foreach (Type type in GenTypes.AllTypes)
//                    {
//                        if (type.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() == null && !type.FullName.Contains("DubsAnalyzer"))
//                        {
//                            foreach (MethodInfo meth in type.GetMethods())
//                            {
//                                if (meth.DeclaringType == type && !meth.IsSpecialName && !meth.IsAssembly && meth.HasMethodBody())
//                                {
//                                    string str = string.Concat(meth.DeclaringType, ":", meth.Name);
//                                    if (str.Contains(searchText))
//                                        names.Add(str);
//                                }
//                            }
//                        }
//                    }


//                    lock (sync)
//                    {
//                        cachedEntries = names;
//                        curSearching = false;
//                    }
//                }

//                private static void PopulateSearchType(string searchText)
//                {
//                    if (searchText.Length <= 2) return;

//                    lock (sync)
//                    {
//                        curSearching = true;
//                    }

//                    HashSet<string> names = new HashSet<string>();
//                    foreach (Type type in GenTypes.AllTypes)
//                    {
//                        if (type.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() == null)
//                        {
//                            if (type.FullName.Contains(searchText) && !type.FullName.Contains("DubsAnalyzer"))
//                                names.Add(type.FullName);
//                        }
//                    }
//                    lock (sync)
//                    {
//                        cachedEntries = names;
//                        curSearching = false;
//                    }
//                }

//                private static void PopulateSearchAssembly(string searchText)
//                {
//                    lock (sync)
//                    {
//                        curSearching = true;
//                    }

//                    HashSet<string> names = new HashSet<string>();
//                    foreach (string mod in ModInfoCache.AssemblyToModname.Values)
//                    {
//                        if (mod.ToLower().Contains(searchText.ToLower()))
//                            names.Add(mod);
//                    }
//                    lock (sync)
//                    {
//                        cachedEntries = names;
//                        curSearching = false;
//                    }
//                }

//                public static void DrawSearchBar(Rect rect)
//                {
//                    rect.height = Text.LineHeight * 6;

//                    Rect baseRect = rect.AtZero();
//                    baseRect.y += Text.LineHeight;
//                    baseRect.height = yHeigthCache;

//                    Widgets.BeginScrollView(rect, ref searchpos, baseRect, false);
//                    GUI.BeginGroup(baseRect);
//                    listing.Begin(baseRect);

//                    float yHeight = 0;

//                    Text.Anchor = TextAnchor.MiddleLeft;
//                    Text.Font = GameFont.Tiny;

//                    int count = 0;
//                    foreach (string entry in cachedEntries)
//                    {
//                        count++;
//                        if (count == 50)
//                        {
//                            break;
//                        }

//                        Rect r = listing.GetRect(Text.LineHeight);

//                        if (Widgets.ButtonInvisible(r))
//                            currentInput = entry;

//                        Widgets.DrawBoxSolid(r, Modbase.Settings.GraphCol);

//                        r.width = 2000;
//                        Widgets.Label(r, " " + entry);
//                        listing.GapLine(0f);
//                        yHeight += 4f;
//                        yHeight += r.height;
//                    }

//                    listing.End();
//                    yHeigthCache = yHeight;
//                    GUI.EndGroup();

//                    DubGUI.ResetFont();
//                    Widgets.EndScrollView();
//                }
//            }
//        }

//    }