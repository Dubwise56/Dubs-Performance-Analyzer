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
using Verse;

namespace Analyzer.Profiling
{
    public class Window_SearchBar : Window
    {
        public string currentInput;
        public Rect rect;

        public Rect viewFrustum;
        public Thread searchThread = null;
        public HashSet<string> cachedEntries = new HashSet<string>();
        public bool curSearching = false;
        public string prevInput = "";
        public object sync = new object();
        private float yHeigthCache = float.MaxValue;
        private static Vector2 searchpos = Vector2.zero;
        public Listing_Standard listing = new Listing_Standard();

        public Window_SearchBar(string currentInput, Rect pos)
        {
            this.currentInput = currentInput;
            this.windowRect = pos;

            closeOnClickedOutside = true;
            doWindowBackground = false;
            drawShadow = false;
            preventCameraMotion = false;

            PopulateSearch(currentInput, Panel_DevOptions.input);
        }

        public override void SetInitialSizeAndPosition()
        {
            windowRect = rect;
        }

        public void PopulateSearch(string searchText, Panel_DevOptions.CurrentInput inputType)
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
                    case Panel_DevOptions.CurrentInput.Method:
                    case Panel_DevOptions.CurrentInput.InternalMethod:
                    case Panel_DevOptions.CurrentInput.MethodHarmony:
                        searchThread = new Thread(() => PopulateSearchMethod(searchText));
                        break;
                    case Panel_DevOptions.CurrentInput.Type:
                    case Panel_DevOptions.CurrentInput.TypeHarmony:
                    case Panel_DevOptions.CurrentInput.SubClasses:
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
        }

        private void PopulateSearchMethod(string searchText)
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
                if (type.Namespace.Contains("Cecil") || type.Namespace.Contains("Analyzer")) continue;

                if (type.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() == null)
                {
                    foreach (MethodInfo meth in type.GetMethods())
                    {
                        if (meth.DeclaringType == type && !meth.IsSpecialName && !meth.IsAssembly && meth.HasMethodBody())
                        {
                            string strf = string.Concat(meth.DeclaringType, ":", meth.Name);
                            string str = strf;
                            if (str.ToLower()
                                .Contains(searchText))
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

        private void PopulateSearchType(string searchText)
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
                    var tyName = type.FullName;
                    if (type.FullName.ToLower()
                        .Contains(searchText) && !type.FullName.Contains("Analyzer"))
                        names.Add(tyName);
                }
            }

            lock (sync)
            {
                cachedEntries = names;
                curSearching = false;
            }
        }

        private void PopulateSearchAssembly(string searchText)
        {
            lock (sync)
            {
                curSearching = true;
            }

            var names = new HashSet<string>();
            foreach (string mod in ModInfoCache.AssemblyToModname.Values)
            {
                var modname = mod;
                if (mod.ToLower()
                    .Contains(searchText.ToLower()))
                    names.Add(modname);
            }

            lock (sync)
            {
                cachedEntries = names;
                curSearching = false;
            }
        }

        public override void DoWindowContents(Rect uselessRect)
        {
            if (GUI.GetNameOfFocusedControl() != currentInput) return;

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
                if (!(cachedEntries.Count == 1 && cachedEntries.First() == currentInput))
                {
                    foreach (var entry in cachedEntries)
                    {
                        var r = listing.GetRect(Text.LineHeight);

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
                            {
                                currentInput = entry;
                                Event.current.Use();
                            }

                            GUI.DrawTexture(r.RightPartPixels(r.height), ResourceCache.GUI.enter);
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

