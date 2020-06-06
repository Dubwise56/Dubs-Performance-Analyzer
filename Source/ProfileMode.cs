using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using Verse;

namespace DubsAnalyzer
{
    [AttributeUsage(AttributeTargets.Field)]
    public class Setting : Attribute
    {
        public string name;
        public string tip;
        public Setting(string name, string tip = null)
        {
            this.name = name;
            this.tip = tip.Translate();
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class PerformancePatch : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ProfileMode : Attribute
    {
        public static List<ProfileMode> instances = new List<ProfileMode>();

        public bool IsPatched = false;
        public Dictionary<FieldInfo, Setting> Settings = new Dictionary<FieldInfo, Setting>();

        public string name;
        public string tip;

        public UpdateMode mode;
        public bool Active = false;
        public Type typeRef;
        public MethodInfo MouseOver;
        public MethodInfo Selected;
        public MethodInfo Clicked;
        public MethodInfo Checkbox;
        public MethodInfo DoRow;
        public bool Basics = false;
        public Thread Patchinator = null;

        public void SetActive(bool b)
        {
            if (typeRef != null)
            {
                AccessTools.Field(typeRef, "Active")?.SetValue(null, b);
            }

            Active = b;
        }

        public static ProfileMode Create(string name, UpdateMode mode, string tip = null, bool basics = false, Type profilerClass = null)
        {
            var getit = instances.FirstOrDefault(x => x.name == name && x.mode == mode);
            if (getit != null)
            {
                return getit;
            }
            getit = new ProfileMode(name, mode, tip, basics);
            getit.typeRef = profilerClass;
            instances.Add(getit);
            return getit;
        }
        public ProfileMode(string name, UpdateMode mode, string tip = null, bool Basics = false)
        {
            this.name = name;
            this.mode = mode;
            this.tip = tip.Translate();
            this.Basics = Basics;
        }
        public void Start(string key)
        {
            if (Active) Analyzer.Start(key);
        }
        public void Start(string key, MethodInfo info)
        {
            if (Active) Analyzer.Start(key, null, null, null, null, info);
        }

        public void Stop(string key)
        {
            if (Active) Analyzer.Stop(key);
        }
        public void ProfilePatch()
        {
            if (Patchinator == null)
            {
                if (AnalyzerState.State == CurrentState.Unpatching) // We are currently unpatching methods, we should not be currently patching more methods
                    return;

                Patchinator = new Thread(() =>
                {                     
                    try
                    {
                        AccessTools.Method(typeRef, "ProfilePatch")?.Invoke(null, null);
                        IsPatched = true;
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.ToString());
                    }
                });
                Patchinator.Start();
            }
        }
    }
}