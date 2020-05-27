using System;
using System.Collections.Generic;

namespace DubsAnalyzer
{
    public class ProfileTab
    {
        public Func<bool> selectedGetter;
        public UpdateMode UpdateMode;
        public string label;
        public Action clickedAction;
        public Dictionary<ProfileMode, Type> Modes = new Dictionary<ProfileMode, Type>();
        public bool Selected => selectedGetter?.Invoke() ?? false;
        public string Tip;
        public bool Collapsed = false;

        public ProfileTab(string label, Action clickedAction, Func<bool> selected, UpdateMode um, string Tip)
        {
            this.label = label;
            this.clickedAction = clickedAction;
            selectedGetter = selected;
            UpdateMode = um;
            this.Tip = Tip;
        }
    }
}