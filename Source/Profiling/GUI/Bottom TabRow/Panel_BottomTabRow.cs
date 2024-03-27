using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Analyzer.Profiling
{
    public struct GeneralInformation
    {
        public MethodBase method;
        public string modName;
        public string assname;
        public string methodName;
        public string typeName;
        public string patchType;
        public List<GeneralInformation> patches;
    }
    
    interface IBottomTabRow
    {
        public abstract void Draw(Rect rect, GeneralInformation? information);
        public abstract void ResetState(GeneralInformation? info);
    }
}