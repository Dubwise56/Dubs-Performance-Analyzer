using System;
using System.Collections.Generic;

namespace Analyzer
{
    public enum UpdateMode { Update, Tick }
    public class Tab
    {
        public Action onClick;
        public Func<bool> onSelect;
        public bool Selected => onSelect?.Invoke() ?? false;

        public Category category;
        public string label;
        public string tip;
        public bool collapsed = false;

        public Dictionary<Entry, Type> entries = new Dictionary<Entry, Type>();

        public Tab(string label, Action onClick, Func<bool> onSelect, Category category, string tip)
        {
            this.label = label;
            this.onClick = onClick;
            this.onSelect = onSelect;
            this.category = category;
            this.tip = tip;
        }
    }
}