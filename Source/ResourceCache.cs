using Analyzer.Profiling;
using UnityEngine;
using Verse;

namespace Analyzer
{
    [StaticConstructorOnStartup]
    public static class Textures
    {
        public static readonly Texture2D
            black = SolidColorMaterials.NewSolidColorTexture(Color.black),
            grey = SolidColorMaterials.NewSolidColorTexture(Color.grey),
            lightGrey = SolidColorMaterials.NewSolidColorTexture(new(0.67f, 0.67f, 0.67f, 1f)),
            lighterGrey = SolidColorMaterials.NewSolidColorTexture(new(0.83f, 0.83f, 0.83f, 1f)),
            white = SolidColorMaterials.NewSolidColorTexture(Color.white),
            darkgrey = SolidColorMaterials.NewSolidColorTexture(Color.grey * 0.5f),
            clear = SolidColorMaterials.NewSolidColorTexture(Color.clear),
            red = SolidColorMaterials.NewSolidColorTexture(new Color32(160, 80, 90, 255)),
            blue = SolidColorMaterials.NewSolidColorTexture(new Color32(130, 123, 190, 255)),
            yellow = SolidColorMaterials.NewSolidColorTexture(new Color32(175, 160, 40, 225)),
            green = SolidColorMaterials.NewSolidColorTexture(new Color32(40, 200, 40, 127));

        public static readonly Texture2D hueMark = ContentFinder<Texture2D>.Get("DPA/UI/hueMark");
        public static readonly Texture2D hsbMark = ContentFinder<Texture2D>.Get("DPA/UI/hsbMark");

        public static Texture2D Gear = ContentFinder<Texture2D>.Get("DPA/UI/MenuSett", false);
        public static Texture2D MintSearch = ContentFinder<Texture2D>.Get("DPA/UI/MintSearch", false);
        public static Texture2D DropDown = ContentFinder<Texture2D>.Get("DPA/UI/dropdown", false);
        public static Texture2D FoldUp = ContentFinder<Texture2D>.Get("DPA/UI/foldup", false);
        public static Texture2D sav = ContentFinder<Texture2D>.Get("DPA/UI/sav", false);
        public static Texture2D disco = ContentFinder<Texture2D>.Get("DPA/UI/discord", false);
        public static Texture2D Support = ContentFinder<Texture2D>.Get("DPA/UI/Support", false);
        public static Texture2D enter = ContentFinder<Texture2D>.Get("DPA/UI/enter", false);
        public static Texture2D refresh = ContentFinder<Texture2D>.Get("DPA/UI/Refresh", false);
        public static Texture2D pin = ContentFinder<Texture2D>.Get("DPA/UI/Pin", false);
        public static Texture2D Burger = ContentFinder<Texture2D>.Get("DPA/UI/billButt", false);
        
        public static Texture2D clearbg = ContentFinder<Texture2D>.Get("DPA/UI/Clear", false);
        public static Texture2D savebg = ContentFinder<Texture2D>.Get("DPA/UI/Save", false);
        public static Texture2D stoppg = ContentFinder<Texture2D>.Get("DPA/UI/Stop", false);
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
        public static string settings_enable_debug_log => "settings.enable.debug.log".Tr();
        public static string settings_show_icon => "settings.show.icon".Tr();
        public static string settings_long_form_names => "settings.long.form.names".Tr();
        public static string settings_disable_threading => "settings.disable.threading".Tr();


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
        public static string logs_maxpu(string cycle) => "logs.maxpu".Translate(cycle);
        public static string logs_av => "logs.av".Tr();
        public static string logs_avpu(string cycle) => "logs.avpu".Translate(cycle);
        public static string logs_percent => "logs.percent".Tr();
        public static string logs_avpc => "logs.avpc".Tr();
        public static string logs_calls => "logs.calls".Tr();
        public static string logs_name => "logs.name".Tr();
        public static string logs_total => "logs.total".Tr();
        public static string logs_callspu(string cycle) => "logs.callspu".Translate(cycle);
        
        public static string Frame => "dpa.frame".Tr();

        public static string logs_max_desc => "logs.max.desc".Tr();
        public static string logs_av_desc => "logs.av.desc".Tr();
        public static string logs_percent_desc => "logs.percent.desc".Tr();
        public static string logs_avpc_desc => "logs.avpc.desc".Tr();
        public static string logs_calls_desc => "logs.calls.desc".Tr();
        public static string logs_name_desc => "logs.name.desc".Tr();
        public static string logs_total_desc => "logs.total.desc".Tr();
        public static string logs_callspu_desc(string cycle) => "logs.callspu.desc".Translate(cycle);
        
        // Bottom tab panels

        public static string panel_mod_name => "panel.mod.name".Tr();
        public static string panel_patch_type => "panel.patch.type".Tr();
        public static string panel_opengithub => "panel.opengithub".Tr();
        public static string panel_opendnspy => "panel.opendnspy".Tr();

        // Frame Profiler buttons
        public static string frameprofiler_button => "frameprofiler.button".Tr();
        public static string frameprofiler_open => "frameprofiler.open".Tr();
        public static string frameprofiler_button_tip => "frameprofiler.button.tip".Tr();

        // Patches tab
        public static string tab_patches => "tab.patches".Tr();
        public static string tab_patches_desc => "tab.patches.desc".Tr();

        // Log columns
        public static string logs_spikes => "logs.spikes".Tr();
        public static string logs_spikes_desc => "logs.spikes.desc".Tr();
        public static string logs_header_percenttotal(string pct) => "logs.header.percenttotal".Translate(pct);
        public static string logs_header_mstotal(string ms) => "logs.header.mstotal".Translate(ms);
        public static string logs_header_maxtotal(string ms) => "logs.header.maxtotal".Translate(ms);
        public static string logs_header_sorted => "logs.header.sorted".Tr();
        public static string logs_header_clicktosort => "logs.header.clicktosort".Tr();
        public static string logs_rowbar_tip(string self, string selfPct, string avg, string avgPct, string entries) => "logs.rowbar.tip".Translate(self, selfPct, avg, avgPct, entries);

        // Where a patch ran
        public static string phase_tick => "phase.tick".Tr();
        public static string phase_update => "phase.update".Tr();
        public static string phase_interface => "phase.interface".Tr();
        public static string phase_threads => "phase.threads".Tr();
        public static string phase_none => "phase.none".Tr();

        // Root and frame map
        public static string unit_tick => "unit.tick".Tr();
        public static string unit_ticks => "unit.ticks".Tr();
        public static string unit_frame => "unit.frame".Tr();
        public static string unit_frames => "unit.frames".Tr();
        public static string root_frame_title => "root.frame.title".Tr();
        public static string root_frame_noun => "root.frame.noun".Tr();
        public static string root_tick_title => "root.tick.title".Tr();
        public static string root_tick_noun => "root.tick.noun".Tr();
        public static string root_update_title => "root.update.title".Tr();
        public static string root_update_noun => "root.update.noun".Tr();
        public static string root_heading(string title, string ms, string unit, string samples, string units, string listed, string pct) => "root.heading".Translate(title, ms, unit, samples, units, listed, pct);
        public static string root_heading_tip(string samples, string units, string noun, string listed) => "root.heading.tip".Translate(samples, units, noun, listed);
        public static string root_segment_tip(string label, string ms, string unit, string pct, string noun) => "root.segment.tip".Translate(label, ms, unit, pct, noun);
        public static string root_otherrows(string count) => "root.otherrows".Translate(count);
        public static string root_rest_tip(string noun) => "root.rest.tip".Translate(noun);
        public static string frame_tip_poll => "frame.tip.poll".Tr();
        public static string frame_tip_update => "frame.tip.update".Tr();
        public static string frame_tip_capture => "frame.tip.capture".Tr();
        public static string frame_tip_other => "frame.tip.other".Tr();
        public static string frame_tip_waiting => "frame.tip.waiting".Tr();
        public static string frame_waiting => "frame.waiting".Tr();
        public static string frame_segment_tip(string tip, string ms, string pct) => "frame.segment.tip".Translate(tip, ms, pct);
        public static string frame_ticksperframe(string ticks) => "frame.ticksperframe".Translate(ticks);
        public static string frame_heading(string ms, string fps, string samples) => "frame.heading".Translate(ms, fps, samples);
        public static string frame_heading_tip => "frame.heading.tip".Tr();
        public static string frame_label_whole => "frame.label.whole".Tr();
        public static string frame_label_ticks => "frame.label.ticks".Tr();
        public static string frame_label_update => "frame.label.update".Tr();
        public static string frame_label_tab => "frame.label.tab".Tr();
        public static string frame_root_tip(string title, string ms, string unit, string ticks, string frameMs, string pct) => "frame.root.tip".Translate(title, ms, unit, ticks, frameMs, pct);
        public static string frame_row_tip(string label, string ms, string unit, string ticks, string frameMs, string rootPct, string noun, string framePct) => "frame.row.tip".Translate(label, ms, unit, ticks, frameMs, rootPct, noun, framePct);
        public static string frame_tab_heading(string title, string ms, string unit, string samples, string units, string listed, string pct) => "frame.tab.heading".Translate(title, ms, unit, samples, units, listed, pct);
        public static string frame_tab_heading_tip(string samples, string units, string noun, string listed) => "frame.tab.heading.tip".Translate(samples, units, noun, listed);

        // Spike flags
        public static string spike_every(string interval, string units) => "spike.every".Translate(interval, units);
        public static string spike_one(string entries, string units, string worst, string usual) => "spike.one".Translate(entries, units, worst, usual);
        public static string spike_many(string count, string entries, string units, string worst, string usual) => "spike.many".Translate(count, entries, units, worst, usual);
        public static string spike_notinterval => "spike.notinterval".Tr();
        public static string spike_heavy(string usual) => "spike.heavy".Translate(usual);
        public static string spike_light(string usual) => "spike.light".Translate(usual);

        // Stack traces
        public static string stack_trace => "stack.trace".Tr();
        public static string stack_trace_tip => "stack.trace.tip".Tr();
        public static string stack_status_tracing(string method, string captured, string paths) => "stack.status.tracing".Translate(method, captured, paths);
        public static string stack_status_stopped(string captured, string paths) => "stack.status.stopped".Translate(captured, paths);
        public static string stack_status_idle => "stack.status.idle".Tr();
        public static string stack_splitter_tip => "stack.splitter.tip".Tr();
        public static string stack_samples_hint => "stack.samples.hint".Tr();
        public static string stack_samples_selected(string distance) => "stack.samples.selected".Translate(distance);
        public static string stack_sample_tip(string distance, string units, string ms, string calls) => "stack.sample.tip".Translate(distance, units, ms, calls);
        public static string stack_sample_none => "stack.sample.none".Tr();
        public static string stack_sample_click => "stack.sample.click".Tr();
        public static string stack_title_sample(string distance, string units, string calls) => "stack.title.sample".Translate(distance, units, calls);
        public static string stack_title_ontype(string type) => "stack.title.ontype".Translate(type);
        public static string stack_title_sampled => "stack.title.sampled".Tr();
        public static string stack_title_all => "stack.title.all".Tr();
        public static string stack_title_alltype(string type) => "stack.title.alltype".Translate(type);
        public static string stack_title_none => "stack.title.none".Tr();
        public static string stack_col_calls => "stack.col.calls".Tr();
        public static string stack_col_patches => "stack.col.patches".Tr();
        public static string stack_col_calledfrom => "stack.col.calledfrom".Tr();
        public static string stack_col_method => "stack.col.method".Tr();
        public static string stack_col_mod => "stack.col.mod".Tr();
        public static string stack_table_tip => "stack.table.tip".Tr();
        public static string stack_select => "stack.select".Tr();
        public static string stack_full => "stack.full".Tr();
        public static string stack_menu_profile(string method, string category) => "stack.menu.profile".Translate(method, category);
        public static string stack_menu_internal(string method, string category) => "stack.menu.internal".Translate(method, category);
        public static string stack_menu_patches(string method, string category) => "stack.menu.patches".Translate(method, category);
        public static string stack_menu_copy => "stack.menu.copy".Tr();
        public static string stack_menu_dnspy => "stack.menu.dnspy".Tr();
    }
}