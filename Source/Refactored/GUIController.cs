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
        private static Profiler currentProfile;

        public static Profiler GetCurrentProfile() => currentProfile;
        public static Tab GetCurrentTab() => currentTab;
        public static Category GetCurrentCategory() => currentCategory;

        static GUIController()
        {
            tabs = new List<Tab>
            {
                new Tab("Settings",  () => { currentCategory = Category.Settings; },     () => currentCategory == Category.Settings,        UpdateMode.Update,     "Settings and utils"),
                new Tab("Tick",      () => { currentCategory = Category.Tick; },         () => currentCategory == Category.Tick,            UpdateMode.Tick,       "Things that run on tick"),
                new Tab("Update",    () => { currentCategory = Category.Update; },       () => currentCategory == Category.Update,          UpdateMode.Update,     "Things that run per frame"),
                new Tab("GUI",       () => { currentCategory = Category.GUI; },          () => currentCategory == Category.GUI,             UpdateMode.Update,     "Things that run on GUI"),
                new Tab("Modder",    () => { currentCategory = Category.Settings; },     () => currentCategory == Category.Settings,        UpdateMode.Update,     "Categories that modders have added")
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
