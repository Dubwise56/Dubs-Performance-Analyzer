using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Analyzer
{
    public enum Category { Settings, Tick, Update, GUI, Modder }

    public static class GUIController
    {
        private static Tab currentTab;
        private static Entry currentEntry;
        private static Profiler currentProfiler;
        private static Category currentCategory = Category.Settings;

        private static Dictionary<Category, Tab> tabs;

        public static Profiler CurrentProfiler { get { return currentProfiler; } set { currentProfiler = value; } }
        public static Tab GetCurrentTab => currentTab;
        public static Category GetCurrentCategory => currentCategory;
        public static Entry CurrentEntry => currentEntry;

        public static IEnumerable<Tab> Tabs => tabs.Values;
        public static Tab Tab(Category cat) => tabs[cat];

        public static void InitialiseTabs()
        {
            tabs = new Dictionary<Category, Tab>();

            addTab(() => ResourceCache.Strings.tab_setting, () => ResourceCache.Strings.tab_setting_desc, Category.Settings);
            addTab(() => ResourceCache.Strings.tab_tick, () => ResourceCache.Strings.tab_tick_desc, Category.Tick);
            addTab(() => ResourceCache.Strings.tab_update, () => ResourceCache.Strings.tab_update_desc, Category.Update);
            addTab(() => ResourceCache.Strings.tab_gui, () => ResourceCache.Strings.tab_gui_desc, Category.GUI);
            addTab(() => ResourceCache.Strings.tab_modder, () => ResourceCache.Strings.tab_modder_desc, Category.Modder);

            void addTab(Func<string> name, Func<string> desc, Category cat)
            {
                tabs.Add(cat, new Tab(name, () => currentCategory = cat, () => currentCategory == cat, cat, desc));
            }
        }

        public static void SwapToEntry(string entryName)
        {
            if(currentEntry != null)
            {
                currentEntry.SetActive(false);
                ProfileController.Profiles.Clear();
                Analyzer.RefreshLogCount();
            }

            currentEntry = Tabs
                .Where(t => t.entries.Keys
                    .Any(e => e.name == entryName))
                .First().entries
                    .First(e => e.Key.name == entryName).Key;

            if(!currentEntry.isPatched)
            {
                currentEntry.PatchMethods();
                currentEntry.SetActive(true);
            }
        }

        public static void AddEntry(string name, string tabName, Category updateMode)
        {

        }

        public static void RemoveEntry(string name)
        {

        }


    }
}
