using Analyzer.Profiling;
using UnityEngine;
using Verse;

namespace Analyzer
{
    [StaticConstructorOnStartup]
    public static class Textures
    {
        public static readonly Texture2D black = SolidColorMaterials.NewSolidColorTexture(Color.black);
        public static readonly Texture2D grey = SolidColorMaterials.NewSolidColorTexture(Color.grey);
        public static readonly Texture2D darkgrey = SolidColorMaterials.NewSolidColorTexture(Color.grey * 0.5f);
        public static readonly Texture2D clear = SolidColorMaterials.NewSolidColorTexture(Color.clear);

        public static readonly Texture2D red =
            SolidColorMaterials.NewSolidColorTexture(new Color32(160, 80, 90, 255));

        public static readonly Texture2D blue =
            SolidColorMaterials.NewSolidColorTexture(new Color32(80, 123, 160, 255));

        public static readonly Texture2D hueMark = ContentFinder<Texture2D>.Get("DPA/UI/hueMark");
        public static readonly Texture2D hsbMark = ContentFinder<Texture2D>.Get("DPA/UI/hsbMark");

        public static Texture2D Menu = ContentFinder<Texture2D>.Get("DPA/UI/MenuSett", false);
        public static Texture2D MintSearch = ContentFinder<Texture2D>.Get("DPA/UI/MintSearch", false);
        public static Texture2D DropDown = ContentFinder<Texture2D>.Get("DPA/UI/dropdown", false);
        public static Texture2D FoldUp = ContentFinder<Texture2D>.Get("DPA/UI/foldup", false);
        public static Texture2D sav = ContentFinder<Texture2D>.Get("DPA/UI/sav", false);
        public static Texture2D disco = ContentFinder<Texture2D>.Get("DPA/UI/discord", false);
        public static Texture2D Support = ContentFinder<Texture2D>.Get("DPA/UI/Support", false);
        public static Texture2D enter = ContentFinder<Texture2D>.Get("DPA/UI/enter", false);
        public static Texture2D refresh = ContentFinder<Texture2D>.Get("DPA/UI/Refresh", false);
    }

    public static class Strings // May want to disable Code Lens for the formatting here...
    {
        // Tabs
        public static string tab_setting => "tab.settings".Tr();
        public static string tab_tick => "tab.tick".Tr();
        public static string tab_update => "tab.update".Tr();
        public static string tab_gui => "tab.gui".Tr();
        public static string tab_modder => "tab.modder".Tr();

        public static string tab_setting_desc => "tab.settings.desc".Tr();
        public static string tab_tick_desc => "tab.tick.desc".Tr();
        public static string tab_update_desc => "tab.update.desc".Tr();
        public static string tab_gui_desc => "tab.gui.desc".Tr();
        public static string tab_modder_desc => "tab.modder.desc".Tr();

        // Settings
        public static string settings_wiki => "settings.wiki".Tr();
        public static string settings_discord => "settings.discord".Tr();
        public static string settings_dnspy => "settings.dnspy".Tr();
        public static string settings_updates_per_second => "settings.ups".Tr();
        public static string settings_logging => "settings.logging".Tr();
        public static string settings_disable_cleanup => "settings.disable.cleanup".Tr();
        public static string settings_disable_cleanup_desc => "settings.disable.cleanup.desc".Tr();
        public static string settings_disable_tps_counter => "settings.disable.tps.counter".Tr();

        // Dev Options
        public static string devoptions_input_method => "devoptions.input.method".Tr();
        public static string devoptions_input_methodinternal => "devoptions.input.methodinternal".Tr();
        public static string devoptions_input_methodharmony => "devoptions.input.methodharmony".Tr();
        public static string devoptions_input_type => "devoptions.input.type".Tr();
        public static string devoptions_input_subclasses => "devoptions.input.subclasses".Tr();
        public static string devoptions_input_typeharmony => "devoptions.input.typeharmony".Tr();
        public static string devoptions_input_assembly => "devoptions.input.assembly".Tr();

        // Top Row
        public static string top_pause_analyzer => "top.pause.analyzer".Tr();
        public static string top_refresh => "top.refresh".Tr();
        public static string top_search => "top.search".Tr();
        public static string top_gc_tip => "top.gc.tip".Tr();
        public static string top_fps_tip => "top.fps.tip".Tr();
        public static string top_tps_tip => "top.tps.tip".Tr();

        // Logs Row
        public static string logs_max => "logs.max".Tr();
        public static string logs_av => "logs.av".Tr();
        public static string logs_percent => "logs.percent".Tr();
        public static string logs_total => "logs.total".Tr();
        public static string logs_calls => "logs.calls".Tr();
        public static string logs_name => "logs.name".Tr();

        public static string logs_max_desc => "logs.max.desc".Tr();
        public static string logs_av_desc => "logs.av.desc".Tr();
        public static string logs_percent_desc => "logs.percent.desc".Tr();
        public static string logs_total_desc => "logs.total.desc".Tr();
        public static string logs_calls_desc => "logs.calls.desc".Tr();
        public static string logs_name_desc => "logs.name.desc".Tr();
    }
}