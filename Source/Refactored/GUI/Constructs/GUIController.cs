using HarmonyLib;
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
        public static HashSet<string> types = new HashSet<string>();

        public static Profiler CurrentProfiler { get { return currentProfiler; } set { currentProfiler = value; } }
        public static Tab GetCurrentTab => currentTab;
        public static Category GetCurrentCategory => currentCategory;
        public static Entry CurrentEntry => currentEntry;

        public static IEnumerable<Tab> Tabs => tabs.Values;
        public static Tab Tab(Category cat) => tabs[cat];
        public static Entry EntryByName(string name) => Tabs.Where(t => t.entries.Keys.Any(e => e.name == name)).First().entries.First(e => e.Key.name == name).Key;

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

        public static void ClearEntries()
        {
            foreach (var tab in tabs.Values)
            {
                foreach (var entry in tab.entries.Keys)
                {
                    if (entry.isClosable)
                    {
                        RemoveEntry(entry.name);
                        continue; // already set to unpatched + inactive here
                    }
                    entry.isPatched = false;
                }
            }
        }


        public static void SwapToEntry(string entryName)
        {
            if (currentEntry != null)
            {
                currentEntry.SetActive(false);
                ProfileController.Profiles.Clear();
                Analyzer.RefreshLogCount();
                currentProfiler = null;
            }

            currentEntry = EntryByName(entryName);

            if (!currentEntry.isPatched)
            {
                currentEntry.PatchMethods();
            }

            currentEntry.SetActive(true);
            currentCategory = currentEntry.category;
            currentTab = Tab(currentCategory);
        }

        public static void AddEntry(string name, Category category)
        {
            Type myType = null;
            if (types.Contains(name))
            {
                myType = AccessTools.TypeByName(name);
            }
            else
            {
                myType = DynamicTypeBuilder.CreateType(name, null);
                types.Add(name);
            }

#if DEBUG
            ThreadSafeLogger.Message($"Adding entry {name} into the category {category.ToString()}");
#endif

            GUIController.Tab(category).entries.Add(Entry.Create(myType.Name, category, null, myType, true), myType);
        }

        public static void RemoveEntry(string name)
        {
            var entry = EntryByName(name);
            entry.isPatched = false;
            entry.SetActive(false);

            Tab(entry.category).entries.Remove(entry);

#if DEBUG
            ThreadSafeLogger.Message($"Removing entry {name} from the category {entry.category.ToString()}");
#endif
        }
    }
}
