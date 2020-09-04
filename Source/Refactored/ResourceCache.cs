using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Analyzer
{
    public static class ResourceCache
    {
        public static class GUI
        {
            public static readonly Texture2D black = SolidColorMaterials.NewSolidColorTexture(Color.black);
            public static readonly Texture2D grey = SolidColorMaterials.NewSolidColorTexture(Color.grey);
            public static readonly Texture2D darkgrey = SolidColorMaterials.NewSolidColorTexture(Color.grey * 0.5f);
            public static readonly Texture2D clear = SolidColorMaterials.NewSolidColorTexture(Color.clear);
            public static readonly Texture2D red = SolidColorMaterials.NewSolidColorTexture(new Color32(160, 80, 90, 255));
            public static readonly Texture2D blue = SolidColorMaterials.NewSolidColorTexture(new Color32(80, 123, 160, 255));
            public static readonly Texture2D hueMark = ContentFinder<Texture2D>.Get("DPA/UI/hueMark");
            public static readonly Texture2D hsbMark = ContentFinder<Texture2D>.Get("DPA/UI/hsbMark");

            public static Texture2D MintSearch = ContentFinder<Texture2D>.Get("DPA/UI/MintSearch", false);
            public static Texture2D DropDown = ContentFinder<Texture2D>.Get("DPA/UI/dropdown", false);
            public static Texture2D FoldUp = ContentFinder<Texture2D>.Get("DPA/UI/foldup", false);
            public static Texture2D sav = ContentFinder<Texture2D>.Get("DPA/UI/sav", false);
        }

        public static class Strings
        {
            // Tabs
            public static string tab_setting = "tab.settings".TranslateSimple();
            public static string tab_tick = "tab.tick".TranslateSimple();
            public static string tab_update = "tab.update".TranslateSimple();
            public static string tab_gui = "tab.gui".TranslateSimple();
            public static string tab_modder = "tab.modder".TranslateSimple();

            public static string tab_setting_desc = "tab.settings.desc".TranslateSimple();
            public static string tab_tick_desc = "tab.tick.desc".TranslateSimple();
            public static string tab_update_desc = "tab.update.desc".TranslateSimple();
            public static string tab_gui_desc = "tab.gui.desc".TranslateSimple();
            public static string tab_modder_desc = "tab.modder.desc".TranslateSimple();

            // 
        }

        [DefOf]
        public static class DefOfs
        {

        }

    }
}
