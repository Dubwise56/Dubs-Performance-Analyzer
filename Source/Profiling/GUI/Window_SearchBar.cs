using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using Verse;

namespace Analyzer.Profiling
{
    internal class Window_SearchBar
    {
        internal static Color windowTransparency = new Color(1, 1, 1, 1);
        private const float mouseDistTillClose = 35f;

        public static Rect viewFrustum;
        private static Vector2 scrollOffset = new Vector2(0, 0);

        public static Listing_Standard listing = new Listing_Standard { maxOneColumn = true };

        private static bool requiresUpdate = true;
        private static string searchText;
        private static CurrentInput currentInput;
        public static HashSet<string> cachedEntries = new HashSet<string>();

        public static Thread searchThread = null;
        public static object sync = new object();
        public static bool currentlySearching = false;

        public static void UpdateSearchString(string newString)
        {
            if (newString == searchText) return;

            searchText = newString;
            requiresUpdate = true;
        }

        public static void SetCurrentInput(CurrentInput inputType)
        {
            if (inputType == currentInput) return;

            lock (sync)
            {
                cachedEntries = new HashSet<string>();
            }

            currentInput = inputType;
            requiresUpdate = true;
        }

        public static bool CheckShouldClose(Rect r)
        {
            if (r.Contains(Event.current.mousePosition))
            {
                windowTransparency = new Color(1, 1, 1, 1);
                return false;
            }

            var num = GenUI.DistFromRect(r, Event.current.mousePosition);

            windowTransparency = new Color(1f, 1f, 1f, 1f - (num / mouseDistTillClose));

            return num > mouseDistTillClose;
        }

        public static void DoWindowContents(Rect inRect)
        {
            if (requiresUpdate)
            {
                PopulateSearch(searchText, currentInput);
                requiresUpdate = false;
            }

            if (cachedEntries.Count > 0)
            {
                Draw(inRect);
            }
          
        }

        public static int HighlightedEntry = 0;


        public static void Control()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.DownArrow)
                {
                    Event.current.Use();
                    arrowPressed = true;
                    HighlightedEntry++;
                }
                if (Event.current.keyCode == KeyCode.UpArrow)
                {
                    Event.current.Use();
                    arrowPressed = true;
                    HighlightedEntry--;
                }
                if (Event.current.keyCode == KeyCode.Return)
                {
                    returned = true;
                    Event.current.Use();
                }
            }
        }

        private static bool arrowPressed = false;
        private static bool returned = false;

        public static void Draw(Rect rect)
        {
            rect.height = Mathf.Min(listing.curY+8, rect.height);

            Widgets.DrawBoxSolid(rect, Widgets.WindowBGFillColor);
            rect = rect.ContractedBy(4);

            viewFrustum = rect.AtZero();
            viewFrustum.y = scrollOffset.y;

            var innerRect = rect.AtZero();
            innerRect.height = listing.curY;
            innerRect.width -= 24f;
            innerRect.x += 6;

            Widgets.BeginScrollView(rect, ref scrollOffset, innerRect);
            GUI.BeginGroup(innerRect);
            listing.Begin(innerRect);

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Tiny;

            lock (sync)
            {
                if (HighlightedEntry > cachedEntries.Count - 1)
                {
                    HighlightedEntry = 0;
                }

                if (HighlightedEntry < 0)
                {
                    HighlightedEntry = cachedEntries.Count - 1;
                }

                if (!(cachedEntries.Count == 1 && cachedEntries.First() == searchText))
                {
                    int L = -1;
                    foreach (var entry in cachedEntries)
                    {
                        L++;
                        var r = listing.GetRect(Text.LineHeight);

                        if (arrowPressed)
                        {
                            if (L == HighlightedEntry)
                            {
                                if (r.y < viewFrustum.y)
                                {
                                    scrollOffset.y = r.y;
                                }

                                if (r.yMax + r.height > viewFrustum.yMax)
                                {
                                    scrollOffset.y = r.yMax + r.height - viewFrustum.height;
                                }
                            }
                        }

                        if (!r.Overlaps(viewFrustum)) continue;


                        if (Widgets.ButtonInvisible(r) || L == HighlightedEntry && returned)
                        {
                            Panel_DevOptions.currentInput = entry;
                            GUI.FocusControl("profileinput");
                        }



                        if (Mouse.IsOver(r) || L == HighlightedEntry)
                        {
                            Widgets.DrawHighlight(r);
                        }

                        r.width = 2000;
                        Widgets.Label(r, entry);

                    }
                }
            }

            listing.End();
            GUI.EndGroup();
            Widgets.EndScrollView();

            DubGUI.ResetFont();
            returned = false;
            arrowPressed = false;
        }


        private static void PopulateSearch(string searchText, CurrentInput inputType)
        {
            if (searchText.Length <= 4 && currentInput != CurrentInput.Assembly)
            {
                lock (sync)
                {
                    cachedEntries = new HashSet<string>();
                }
                
                return;
            }

            bool active = false;

            lock (sync)
            {
                active = currentlySearching;
            }

            if (active) return; // Already a thread doing the computation

            switch (inputType)
            {
                case CurrentInput.Method:
                case CurrentInput.InternalMethod:
                case CurrentInput.MethodHarmony:
                    searchThread = new Thread(() => PopulateSearchMethod(searchText));
                    break;
                case CurrentInput.Type:
                case CurrentInput.TypeHarmony:
                case CurrentInput.SubClasses:
                    searchThread = new Thread(() => PopulateSearchType(searchText));
                    break;
                default:
                    searchThread = new Thread(() => PopulateSearchAssembly(searchText));
                    break;
            }

            searchThread.IsBackground = true;
            searchThread.Start();
        }

        private static void PopulateSearchMethod(string searchText)
        {
            lock (sync)
            {
                currentlySearching = true;
            }

            searchText = searchText.ToLower();

            var names = new HashSet<string>();

            foreach (var type in GenTypes.AllTypes)
            {
                if (type.FullName.Contains("Cecil") || type.FullName.Contains("Analyzer")) continue;
                if (type.GetCustomAttribute<CompilerGeneratedAttribute>() != null) continue;


                foreach (var meth in type.GetMethods(AccessTools.all))
                {
                    if (!meth.HasMethodBody()) continue;
                    if (meth.IsGenericMethod || meth.ContainsGenericParameters) continue;

                    var str = string.Concat(meth.DeclaringType, ":", meth.Name);
                    if (str.ToLower()
                        .Contains(searchText))
                        names.Add(str);
                }
            }


            lock (sync)
            {
                cachedEntries = names;
                currentlySearching = false;
            }
        }

        private static void PopulateSearchType(string searchText)
        {
            lock (sync)
            {
                currentlySearching = true;
            }

            searchText = searchText.ToLower();

            var names = new HashSet<string>();
            foreach (var type in GenTypes.AllTypes)
            {
                if (type.GetCustomAttribute<CompilerGeneratedAttribute>() != null) continue;


                var tyName = type.FullName;
                if (type.FullName.ToLower()
                    .Contains(searchText) && !type.FullName.Contains("Analyzer"))
                    names.Add(tyName);
            }

            lock (sync)
            {
                cachedEntries = names;
                currentlySearching = false;
            }
        }

        private static void PopulateSearchAssembly(string searchText)
        {
            var names = new HashSet<string>();
            var mods = ModInfoCache.AssemblyToModname.Values;

            foreach (var mod in mods.Where(mod => mod.ToLower()
                .Contains(searchText.ToLower()))) names.Add(mod);

            lock (sync)
            {
                cachedEntries = names;
                currentlySearching = false;
            }
        }
    }
}