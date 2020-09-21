using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Analyzer
{
    using Profiling;

    public static class Panel_Settings
    {
        public static Listing_Standard listing = new Listing_Standard();
        private static Vector2 scrollPos;

        public static void Draw(Rect rect)
        {
            Rect view = rect.AtZero();
            view.height = rect.height;

            Widgets.BeginScrollView(rect, ref scrollPos, view, false);
            GUI.BeginGroup(view);
            view.height = 9999;
            listing.Begin(view.ContractedBy(10f));

            // Draw the github and discord textures / Icons

            Rect rec = listing.GetRect(24f);
            Rect lrec = rec.LeftHalf();
            rec = rec.RightHalf();
            Widgets.DrawTextureFitted(lrec.LeftPartPixels(40f), Gfx.Support, 1f);
            lrec.x += 40;
            if (Widgets.ButtonText(lrec.LeftPartPixels(ResourceCache.Strings.settings_wiki.GetWidthCached()), ResourceCache.Strings.settings_wiki, false, true))
            {
                Application.OpenURL("https://github.com/Dubwise56/Dubs-Performance-Analyzer/wiki");
            }

            Widgets.DrawTextureFitted(rec.RightPartPixels(40f), Gfx.disco, 1f);
            rec.width -= 40;
            if (Widgets.ButtonText(rec.RightPartPixels(ResourceCache.Strings.settings_discord.GetWidthCached()), ResourceCache.Strings.settings_discord, false, true))
            {
                Application.OpenURL("https://discord.gg/Az5CnDW");
            }


            listing.GapLine();

            DrawDevOptions();

            listing.End();
            GUI.EndGroup();
            Widgets.EndScrollView();
        }

        /* For Dev Tools */

        public static void DrawDevOptions()
        {
            listing.Label(ResourceCache.Strings.settings_heading);
            DubGUI.InputField(listing.GetRect(Text.LineHeight), ResourceCache.Strings.settings_dnspy, ref Settings.PathToDnspy, ShowName: true);
            DubGUI.LabeledSliderFloat(listing, ResourceCache.Strings.settings_updates_per_second, ref Settings.updatesPerSecond, 1.0f, 20.0f);
            DubGUI.Checkbox(ResourceCache.Strings.settings_logging, listing, ref Settings.verboseLogging);
            DubGUI.Checkbox(ResourceCache.Strings.settings_side_panel, listing, ref Settings.sidePanel);

#if DEBUG
            listing.GapLine();

            DubGUI.Checkbox("Enable Grappling-Box Visualisation", listing, ref Settings.showGrapplingBoxes);
#endif

            listing.GapLine();

            DrawOptions(listing);
        }

        public static void DrawOptions(Listing_Standard listing)
        {
            Rect left = listing.GetRect(Text.LineHeight * 11); // the average height of this is ~226, which is 10.2 * Text.LineHeight
            Rect right = left.RightPart(0.48f);
            left = left.LeftPart(0.48f);

            PatchOptions.DrawPatches(left);
            PatchOptions.DrawUnPatches(right);
        }

        internal static class PatchOptions
        {
            public enum CurrentInput { Method, Type, MethodHarmony, TypeHarmony, InternalMethod, Assembly }
            public enum UnPatchType { Method, MethodsOnMethod, Type, InternalMethod, All }

            public static CurrentInput input = CurrentInput.Method;
            public static UnPatchType unPatchType = UnPatchType.Method;
            public static UpdateMode patchType = UpdateMode.Update;

            public static string currentInput = null;
            public static string currentUnPatch = null;
            public static string currentWIP = null;

            public static void DrawPatches(Rect left)
            {
                Listing_Standard lListing = new Listing_Standard();
                lListing.Begin(left);

                DubGUI.CenterText(() => lListing.Label("ProfilePatchMethod".Translate()));

                lListing.GapLine(6);
                DubGUI.OptionalBox(lListing.GetRect(Text.LineHeight + 3), "input.method".Translate(), () => input = CurrentInput.Method, input == CurrentInput.Method);
                DubGUI.OptionalBox(lListing.GetRect(Text.LineHeight + 3), "input.type".Translate(), () => input = CurrentInput.Type, input == CurrentInput.Type);
                DubGUI.OptionalBox(lListing.GetRect(Text.LineHeight + 3), "input.methodharmony".Translate(), () => input = CurrentInput.MethodHarmony, input == CurrentInput.MethodHarmony);
                DubGUI.OptionalBox(lListing.GetRect(Text.LineHeight + 3), "input.typeharmony".Translate(), () => input = CurrentInput.TypeHarmony, input == CurrentInput.TypeHarmony);
                DubGUI.OptionalBox(lListing.GetRect(Text.LineHeight + 3), "input.methodinternal".Translate(), () => input = CurrentInput.InternalMethod, input == CurrentInput.InternalMethod);
                DubGUI.OptionalBox(lListing.GetRect(Text.LineHeight + 3), "input.assembly".Translate(), () => input = CurrentInput.Assembly, input == CurrentInput.Assembly);
                lListing.curY += 2;

                var action = DisplayInputField(lListing);
                lListing.curY += 2;

                Rect box = lListing.GetRect(Text.LineHeight + 3);

                DubGUI.OptionalBox(box.LeftPart(.3f), "patch.type.tick".Translate(), () => patchType = UpdateMode.Tick, patchType == UpdateMode.Tick);
                box = box.RightPart(.65f);
                DubGUI.OptionalBox(box.LeftPart(.4f), "patch.type.update".Translate(), () => patchType = UpdateMode.Update, patchType == UpdateMode.Update);

                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.KeypadEnter)
                {
                    Event.current.Use();
                    ExecutePatch();
                }
                else if (Widgets.ButtonText(box.RightPart(.5f), "patch".Translate()))
                    if (currentInput != null)
                        ExecutePatch();

                lListing.End();

                action(); // our search bar
            }


            public static Action DisplayInputField(Listing_Standard listing)
            {
                string FieldDescription = null;

                switch (input)
                {
                    case CurrentInput.Method: FieldDescription = "Type:Method"; break;
                    case CurrentInput.Type: FieldDescription = "Type"; break;
                    case CurrentInput.MethodHarmony: FieldDescription = "Type:Method"; break;
                    case CurrentInput.TypeHarmony: FieldDescription = "Type"; break;
                    case CurrentInput.InternalMethod: FieldDescription = "Type:Method"; break;
                    case CurrentInput.Assembly: FieldDescription = "Mod or PackageId"; break;
                }

                Rect inputBox = listing.GetRect(Text.LineHeight);
                DubGUI.InputField(inputBox, FieldDescription, ref currentInput, ShowName: true);

                inputBox.height = Text.LineHeight * 12;
                return SearchBar.PopulateSearch(inputBox, currentInput, input);
            }
            public static void ExecutePatch()
            {
                switch (input)
                {
                    case CurrentInput.Method:
                        if (patchType == UpdateMode.Tick)
                        {
                            CustomProfilersTick.PatchMeth(currentInput);
                            GUIController.SwapToEntry("Custom Tick");
                        }
                        else
                        {
                            CustomProfilersUpdate.PatchMeth(currentInput);
                            GUIController.SwapToEntry("Custom Update");
                        }
                        return;
                    case CurrentInput.Type:
                        CustomProfilersUpdate.PatchType(currentInput);
                        GUIController.SwapToEntry("Custom Update");
                        return;
                    case CurrentInput.MethodHarmony:
                        CustomProfilersHarmony.PatchMeth(currentInput);
                        GUIController.SwapToEntry("Custom Harmony");
                        return;
                    case CurrentInput.TypeHarmony:
                        CustomProfilersHarmony.PatchType(currentInput);
                        GUIController.SwapToEntry("Custom Harmony");
                        return;
                    case CurrentInput.InternalMethod:
                        Utility.PatchInternalMethod(currentInput);
                        return;
                    case CurrentInput.Assembly:
                        Utility.PatchAssembly(currentInput);
                        return;
                }
            }
            public static void DrawUnPatches(Rect right)
            {
                Listing_Standard rListing = new Listing_Standard();
                rListing.Begin(right);

                DubGUI.CenterText(() => rListing.Label("UnProfilePatchMethod".Translate()));
                rListing.GapLine(6);
                DubGUI.OptionalBox(rListing.GetRect(Text.LineHeight + 3), "input.unpatchmethod".Translate(), () => unPatchType = UnPatchType.Method, unPatchType == UnPatchType.Method);
                DubGUI.OptionalBox(rListing.GetRect(Text.LineHeight + 3), "input.unpatchmethodsonmethod".Translate(), () => unPatchType = UnPatchType.MethodsOnMethod, unPatchType == UnPatchType.MethodsOnMethod);
                DubGUI.OptionalBox(rListing.GetRect(Text.LineHeight + 3), "input.unpatchtype".Translate(), () => unPatchType = UnPatchType.Type, unPatchType == UnPatchType.Type);
                DubGUI.OptionalBox(rListing.GetRect(Text.LineHeight + 3), "input.unpatchinternalmethod".Translate(), () => unPatchType = UnPatchType.InternalMethod, unPatchType == UnPatchType.InternalMethod);
                DubGUI.OptionalBox(rListing.GetRect(Text.LineHeight + 3), "input.unpatchall".Translate(), () => unPatchType = UnPatchType.All, unPatchType == UnPatchType.All);
                rListing.curY += 2;

                DisplayUnPatchInputField(rListing);
                rListing.curY += 2;
                if (Widgets.ButtonText(rListing.GetRect(Text.LineHeight + 3), "unpatch".Translate()))
                    if (currentInput != null || unPatchType == UnPatchType.All)
                        ExecuteUnPatch();

                rListing.End();
            }

            public static void DisplayUnPatchInputField(Listing_Standard listing)
            {
                string FieldDescription = null;

                switch (unPatchType)
                {
                    case UnPatchType.Method: FieldDescription = "Type:Method"; break;
                    case UnPatchType.MethodsOnMethod: FieldDescription = "Type:Method"; break;
                    case UnPatchType.Type: FieldDescription = "Type"; break;
                    case UnPatchType.InternalMethod: FieldDescription = "Type:Method"; break;
                    case UnPatchType.All: FieldDescription = "N/A"; break;
                }

                Rect inputBox = listing.GetRect(Text.LineHeight);
                DubGUI.InputField(inputBox, FieldDescription, ref currentUnPatch, ShowName: true);
            }

            public static void ExecuteUnPatch()
            {
                //switch (unPatchType)
                //{
                //    case UnPatchType.Method: Utility.UnpatchMethod(currentUnPatch); break;
                //    case UnPatchType.MethodsOnMethod: Utility.UnpatchMethod(currentUnPatch); break;
                //    case UnPatchType.Type: Utility.UnPatchTypePatches(currentUnPatch); break;
                //    case UnPatchType.InternalMethod: Utility.UnpatchInternalMethod(currentUnPatch); break;
                //    case UnPatchType.All: Analyzer.Cleanup(); break;
                //}
            }

            internal static class SearchBar
            {
                public static Rect viewFrustum;
                public static Thread searchThread = null;
                public static HashSet<string> cachedEntries = new HashSet<string>();
                public static bool curSearching = false;
                public static string prevInput = "";
                public static object sync = new object();
                private static float yHeigthCache = float.MaxValue;
                private static Vector2 searchpos = Vector2.zero;
                public static Listing_Standard listing = new Listing_Standard();

                public static Action PopulateSearch(Rect rect, string searchText, CurrentInput inputType)
                {
                    bool active = false;
                    lock (sync)
                    {
                        active = curSearching;
                    }

                    if (!active && prevInput != currentInput)
                    {
                        switch (inputType)
                        {
                            case CurrentInput.Method:
                            case CurrentInput.InternalMethod:
                            case CurrentInput.MethodHarmony:
                                searchThread = new Thread(() => PopulateSearchMethod(searchText));
                                break;
                            case CurrentInput.Type:
                            case CurrentInput.TypeHarmony:
                                searchThread = new Thread(() => PopulateSearchType(searchText));
                                break;
                            default:
                                searchThread = new Thread(() => PopulateSearchAssembly(searchText));
                                break;

                        }
                        searchThread.IsBackground = true;
                        prevInput = currentInput;
                        searchThread.Start();
                    }

                    return () => DrawSearchBar(rect);
                }

                private static void PopulateSearchMethod(string searchText)
                {
                    if (searchText.Length <= 4) return;

                    searchText = searchText.ToLower();

                    lock (sync)
                    {
                        curSearching = true;
                    }

                    HashSet<string> names = new HashSet<string>();

                    foreach (Type type in GenTypes.AllTypes)
                    {
                        if (type.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() == null && !type.FullName.Contains("Analyzer"))
                        {
                            foreach (MethodInfo meth in type.GetMethods())
                            {
                                if (meth.DeclaringType == type && !meth.IsSpecialName && !meth.IsAssembly && meth.HasMethodBody())
                                {
                                    string str = string.Concat(meth.DeclaringType, ":", meth.Name).ToLower();
                                    if (str.Contains(searchText))
                                        names.Add(str);
                                }
                            }
                        }
                    }


                    lock (sync)
                    {
                        cachedEntries = names;
                        curSearching = false;
                    }
                }

                private static void PopulateSearchType(string searchText)
                {
                    if (searchText.Length <= 2) return;

                    searchText = searchText.ToLower();

                    lock (sync)
                    {
                        curSearching = true;
                    }

                    HashSet<string> names = new HashSet<string>();
                    foreach (Type type in GenTypes.AllTypes)
                    {
                        if (type.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
                        {
                            if (type.FullName.ToLower().Contains(searchText) && !type.FullName.Contains("Analyzer"))
                                names.Add(type.FullName);
                        }
                    }
                    lock (sync)
                    {
                        cachedEntries = names;
                        curSearching = false;
                    }
                }

                private static void PopulateSearchAssembly(string searchText)
                {
                    lock (sync)
                    {
                        curSearching = true;
                    }

                    HashSet<string> names = new HashSet<string>();
                    foreach (string mod in ModInfoCache.AssemblyToModname.Values)
                    {
                        if (mod.ToLower().Contains(searchText.ToLower()))
                            names.Add(mod);
                    }
                    lock (sync)
                    {
                        cachedEntries = names;
                        curSearching = false;
                    }
                }

                public static void DrawSearchBar(Rect rect)
                {
                    rect.y += Text.LineHeight * 9.5f; // todo don't hardcore this :facepalm:

                    if (!rect.ExpandedBy(10f).Contains(Event.current.mousePosition)) return;


                    Rect innerRect = rect.AtZero();
                    innerRect.height = yHeigthCache;

                    viewFrustum = rect.AtZero();
                    viewFrustum.y += searchpos.y;


                    Widgets.BeginScrollView(rect, ref searchpos, innerRect, false);
                    GUI.BeginGroup(innerRect);
                    listing.Begin(innerRect);

                    float yHeight = 0;

                    Text.Anchor = TextAnchor.MiddleLeft;
                    Text.Font = GameFont.Tiny;


                    lock (sync)
                    {
                        if (cachedEntries.Count != 1)
                        {
                            foreach (string entry in cachedEntries)
                            {
                                Rect r = listing.GetRect(Text.LineHeight);

                                if (!r.Overlaps(viewFrustum))
                                {
                                    yHeight += (r.height + 4f);
                                    continue;
                                }

                                if (Widgets.ButtonInvisible(r))
                                {
                                    currentInput = entry;
                                }

                                Widgets.DrawBoxSolid(r, Modbase.Settings.GraphCol);

                                if (Mouse.IsOver(r))
                                {
                                    Widgets.DrawHighlight(r);
                                    if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Tab)
                                        currentInput = entry;
                                }
                                r.width = 2000;
                                Widgets.Label(r, " " + entry);

                                yHeight += 4f;
                                yHeight += r.height;
                            }
                        }
                    }

                    yHeigthCache = yHeight;

                    listing.End();
                    GUI.EndGroup();
                    Widgets.EndScrollView();

                    DubGUI.ResetFont();
                }
            }
        }

    }
}
