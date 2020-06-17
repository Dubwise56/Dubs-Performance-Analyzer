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
        public static Texture2D DropDown = ContentFinder<Texture2D>.Get("DPA/UI/dropdown", false);
        public static Texture2D FoldUp = ContentFinder<Texture2D>.Get("DPA/UI/foldup", false);


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

        public static void InlineTripleMessage(string left, string middle, string right, Listing_Standard listing, bool capOff)
        {
            left.Insert(0, " "); right.Insert(0, " ");

            var grongo = Text.CalcHeight(left, listing.ColumnWidth / 3f);
            var gronk = Text.CalcHeight(middle, (listing.ColumnWidth / 3f - 5f));
            var shiela = Text.CalcHeight(right, (listing.ColumnWidth / 2f - 5f));


            var rect = listing.GetRect(Mathf.Max(Mathf.Max(grongo, gronk), shiela));

            var leftRect = rect.LeftPart(.3f);
            var rightRect = rect.RightPart(.3f);
            var middleRect = rect.LeftPart(.3f);
            middleRect.x += (rect.width / 3f) + 5;
            rightRect.x += 5;

            var anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;

            Widgets.Label(leftRect, left);
            Widgets.Label(rightRect, right);
            Widgets.Label(middleRect, middle);
            Text.Anchor = anchor;

            Color color = GUI.color;
            GUI.color = color * new Color(1f, 1f, 1f, 0.4f);
            Widgets.DrawLineVertical(rect.width/3, rect.y, rect.height);
            Widgets.DrawLineVertical(2*(rect.width/3), rect.y, rect.height);
            if (capOff)
                Widgets.DrawLineHorizontal(rect.x, rect.y + rect.height, rect.width);
            GUI.color = color;
        }

        public static void InlineDoubleMessage(string left, string right, Listing_Standard listing, bool capOff)
        {
            left.Insert(0,  " "); right.Insert(0, " ");

            var grongo = Text.CalcHeight(left, listing.ColumnWidth/2);
            var gronk = Text.CalcHeight(right, (listing.ColumnWidth/2 - 5f));

            var rect = listing.GetRect(Mathf.Max(grongo, gronk));

            var anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;

            var leftRect = rect.LeftPart(.5f);
            Widgets.Label(leftRect, left);
            var rightRect = rect.RightPart(.5f);
            rightRect.x += 5;
            Widgets.Label(rightRect, right);

            Text.Anchor = anchor;

            Color color = GUI.color;
            GUI.color = color * new Color(1f, 1f, 1f, 0.4f);
            Widgets.DrawLineVertical(rect.center.x, rect.y, rect.height);
            if (capOff)
                Widgets.DrawLineHorizontal(rect.x, rect.y + rect.height, rect.width);
            GUI.color = color;
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
            var rect = listing.GetRect( Mathf.CeilToInt(s.GetWidthCached() / listing.ColumnWidth) * Text.LineHeight);
            return Checkbox(rect, s, ref checkOn);
        }

        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }

        public static void CenterText(Action action)
        {
            var anch = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            action();
            Text.Anchor = anch;
        }

        public static bool InputField(Rect rect, string name, ref string buff, Texture2D icon = null, int max = 999, bool readOnly = false, bool forceFocus = false, bool ShowName = false)
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
                Widgets.Label(rect.LeftPartPixels(name.GetWidthCached()), name);
                Text.Anchor = TextAnchor.UpperLeft;

                rect2 = rect.RightPartPixels(rect.width - (name.GetWidthCached()+3));
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

        public static void OptionalBox(Rect rect, string value, Action action, bool active)
        {
            if(Widgets.RadioButtonLabeled(rect, value, active))
            {
                action();
            }
        }

        public static void CollapsableHeading(Listing_Standard listing, string label, ref bool Collapsed)
        {
            var Rect = listing.GetRect(30);
            Heading(Rect, label);

            Vector2 loc = new Vector2(Rect.x + Rect.width - Rect.height, Rect.y);
            Vector2 len = new Vector2(Rect.height, Rect.height);
            Rect rect = new Rect(loc, len);

            if (Widgets.ButtonImage(rect, Collapsed ? DropDown : FoldUp))
                Collapsed = !Collapsed;
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