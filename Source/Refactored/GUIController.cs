using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer
{
    public enum Category { Settings, Tick, Update, GUI, Modder }
    public static class GUIController
    {
        private static Tab currentTab;
        private static List<Tab> tabs;
        private static Category currentCategory;
        private static Profiler currentProfiler;

        public static Profiler GetCurrentProfiler() => currentProfiler;
        public static Tab GetCurrentTab() => currentTab;
        public static Category GetCurrentCategory() => currentCategory;

        static GUIController()
        {
            tabs = new List<Tab>
            {
                new Tab(ResourceCache.Strings.tab_setting,   () => { currentCategory = Category.Settings; },     () => currentCategory == Category.Settings,        UpdateMode.Update,     ResourceCache.Strings.tab_setting_desc),
                new Tab(ResourceCache.Strings.tab_tick,      () => { currentCategory = Category.Tick; },         () => currentCategory == Category.Tick,            UpdateMode.Tick,       ResourceCache.Strings.tab_tick_desc),
                new Tab(ResourceCache.Strings.tab_update,    () => { currentCategory = Category.Update; },       () => currentCategory == Category.Update,          UpdateMode.Update,     ResourceCache.Strings.tab_update_desc),
                new Tab(ResourceCache.Strings.tab_gui,       () => { currentCategory = Category.GUI; },          () => currentCategory == Category.GUI,             UpdateMode.Update,     ResourceCache.Strings.tab_gui_desc),
                new Tab(ResourceCache.Strings.tab_modder,    () => { currentCategory = Category.Modder; },       () => currentCategory == Category.Modder,        UpdateMode.Update,       ResourceCache.Strings.tab_modder_desc)
            };
        }

        public static void SwapToEntry(string entryName)
        {

        }

        public static void AddEntry(string name, string tabName, UpdateMode updateMode)
        {

        }

        public static void RemoveEntry(string name)
        {

        }


    }
}
