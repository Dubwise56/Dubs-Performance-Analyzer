using System;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DubsAnalyzer
{
    [StaticConstructorOnStartup]
    public static class DubGUI
    {
        public static Texture2D MintSearch = ContentFinder<Texture2D>.Get("DPA/UI/MintSearch", false);

        public static float ToMb(this long l)
        {
            return l / 1024f / 1024f;
        }

        public static bool Has(this string source, string toCheck,
            StringComparison comp = StringComparison.OrdinalIgnoreCase)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }

        public static void Clear(this StringBuilder value)
        {
            value.Length = 0;
            value.Capacity = 0;
        }


        public static Rect Scale(this Rect rect, float w, float h)
        {
            var biff = new Rect(rect);
            rect.width = w;
            rect.height = h;
            return biff.CenteredOnXIn(rect);
        }

        public static Rect Morph(this Rect rect, float x = 0, float y = 0, float w = 0, float h = 0)
        {
            return rect = new Rect(rect.x + x, rect.y + y, rect.width + w, rect.height + h);
        }

        public static void CopyToClipboard(this string s)
        {
            var te = new TextEditor { text = s };
            te.SelectAll();
            te.Copy();
        }

        public static float SliderLabel(this Listing_Standard listing, string labia, float val, float min, float max)
        {
            var lineHeight = Text.LineHeight;
            var rect = listing.GetRect(lineHeight);

            Text.Font = GameFont.Tiny;
            Widgets.Label(rect.LeftHalf(), labia);
            var valkilmer = Widgets.HorizontalSlider(rect.RightHalf(), val, min, max);
            Text.Font = GameFont.Small;
            listing.Gap(listing.verticalSpacing);
            return valkilmer;
        }


        public static bool Checkbox(Rect rect, string s, ref bool checkOn)
        {
            var br = checkOn;
            if (Widgets.ButtonInvisible(rect))
            {
                checkOn = !checkOn;
                if (checkOn)
                {
                    SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
                }
                else
                {
                    SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
                }
            }

            var anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            //Widgets.CheckboxDraw(rect.x, rect.y, checkOn, false, 15f);

            Widgets.DrawTextureFitted(rect.LeftPartPixels(30f), checkOn ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex, 0.5f);
            rect.x += 30;
            Widgets.Label(rect, s);
            Text.Anchor = anchor;
            if (checkOn != br)
            {
                return true;
            }

            return false;
        }

        public static bool Checkbox(string s, Listing_Standard listing, ref bool checkOn)
        {
            var rect = listing.GetRect(Text.LineHeight);
            return Checkbox(rect, s, ref checkOn);
        }

        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }

        public static bool InputField(Rect rect, string name, ref string buff, Texture2D icon = null, int max = 999,
            bool readOnly = false, bool forceFocus = false, bool ShowName = false)
        {
            if (buff == null)
            {
                buff = "";
            }

            var rect2 = rect;

            if (icon != null)
            {
                var icoRect = rect;
                icoRect.width = icoRect.height;
                Widgets.DrawTextureFitted(icoRect, icon, 1f);
                rect2.width -= icoRect.width;
                rect2.x += icoRect.width;
            }

            if (ShowName)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect.LeftPart(0.2f), name);
                Text.Anchor = TextAnchor.UpperLeft;

                rect2 = rect.RightPart(0.8f);
            }

            GUI.SetNextControlName(name);

            buff = GUI.TextField(rect2, buff, max, Text.CurTextAreaStyle);

            var InFocus = GUI.GetNameOfFocusedControl() == name;

            if (!InFocus && forceFocus)
            {
                GUI.FocusControl(name);
            }

            if (Input.GetMouseButtonDown(0) && !Mouse.IsOver(rect2) && InFocus)
            {
                GUI.FocusControl(null);
            }

            return InFocus;
        }

        public static void ResetFont()
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public static void Heading(Listing_Standard listing, string label)
        {
            Heading(listing.GetRect(30), label);
        }
        public static void Heading(Rect rect, string label)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, label);
            ResetFont();
        }
    }


}