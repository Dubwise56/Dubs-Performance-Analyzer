using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Analyzer
{
    public static class StackTraceRegex
    {

        public static Dictionary<string, StackTraceInformation> traces = new Dictionary<string, StackTraceInformation>();

        private const string strRegex = @"(?<=:)(DMD.*)(?<=::)|(>)"; // Get rid of the garbled error messages that harmony patched methods create   
        private static readonly Regex myRegex = new Regex(strRegex, RegexOptions.None);

        public static void Add(StackTrace trace)
        {
            string key = trace.ToString();

            if(traces.TryGetValue(key, out var value))
                value.Count++;
            else
                traces.Add(key, new StackTraceInformation(trace));
        }

        public static void Reset()
        {
            traces = new Dictionary<string, StackTraceInformation>();
        }

        public static string StackTrace(StackTrace stackTrace)
        {
            // this is from `UnityEngine.StackTraceUtility:ExtractFormattedStackTrace`
            StringBuilder stringBuilder = new StringBuilder(255);
            for (int i = 0; i < stackTrace.FrameCount; i++)
            {
                StackFrame frame = stackTrace.GetFrame(i);
                MethodBase method = frame.GetMethod();

                if (method == null) continue;
                Type declaringType = method.DeclaringType;
                if (declaringType == null) continue;

                string @namespace = declaringType.Namespace;

                if (@namespace != null && @namespace.Length != 0)
                {
                    stringBuilder.Append(@namespace);
                    stringBuilder.Append(".");
                }
                stringBuilder.Append(declaringType.Name);
                stringBuilder.Append(":");
                stringBuilder.Append(method.Name);
                stringBuilder.Append("(");
                ParameterInfo[] parameters = method.GetParameters();
                bool flag = true;
                for (int j = 0; j < parameters.Length; j++)
                {
                    if (!flag)
                    {
                        stringBuilder.Append(", ");
                    }
                    else
                    {
                        flag = false;
                    }
                    stringBuilder.Append(parameters[j].ParameterType.Name);
                }
                stringBuilder.Append(")#");

            }
            return stringBuilder.ToString();
        }

        public static string MethToString(MethodBase method)
        {
            StringBuilder stringBuilder = new StringBuilder(255);
            if (method == null) return "";
            Type declaringType = method.DeclaringType;
            if (declaringType == null) return "";

            string @namespace = declaringType.Namespace;

            if (@namespace != null && @namespace.Length != 0)
            {
                stringBuilder.Append(@namespace);
                stringBuilder.Append(".");
            }
            stringBuilder.Append(declaringType.Name);
            stringBuilder.Append(":");
            stringBuilder.Append(method.Name);
            stringBuilder.Append("(");
            ParameterInfo[] parameters = method.GetParameters();
            bool flag = true;
            for (int j = 0; j < parameters.Length; j++)
            {
                if (!flag)
                {
                    stringBuilder.Append(", ");
                }
                else
                {
                    flag = false;
                }
                stringBuilder.Append(parameters[j].ParameterType.Name);
            }
            stringBuilder.Append(")");

            return ProcessString(stringBuilder.ToString());
        }
        public static string ProcessString(string str)
        {
            string retStr = Regex.Replace(str, "#", "\n");
            retStr = myRegex.Replace(retStr, @"");

            return retStr;
        }

    }

    public class StackTraceInformation
    {
        public class HarmonyPatch
        {
            public HarmonyPatch(MethodInfo patch, HarmonyPatchType type, string id, int index, int priority = -1)
            {
                this.patch = patch; this.type = type; this.id = id; this.index = index; this.priority = priority;
            }
            public MethodInfo patch;
            public HarmonyPatchType type;
            public string id;
            public int index;
            public int priority;
        }

        public StackTraceInformation(StackTrace input)
        {
            ProccessInput(input);
        }

        private void ProccessInput(StackTrace stackTrace)
        {
            // Translate our input into the strings we will want to show the user
            string rawString = StackTraceRegex.StackTrace(stackTrace);
            string processedString = StackTraceRegex.ProcessString(rawString);

            translatedString = processedString;
            translatedStringArr = processedString.Split('\n');

            // Lets get the relevant methods that will be required for interactivity
            for (int i = 0; i < stackTrace.FrameCount; i++)
            {
                StackFrame frame = stackTrace.GetFrame(i);
                MethodBase frameMethod = frame.GetMethod();

                MethodInfo baseMeth = GetBaseMeth(frameMethod as MethodInfo);

                Patches frameMethodPatches = Harmony.GetPatchInfo(baseMeth);
                methods.Insert(i, new Tuple<MethodInfo, List<HarmonyPatch>>(baseMeth, new List<HarmonyPatch>()));
                if (frameMethodPatches != null) // add relevant patch information
                {
                    foreach (Patch patch in frameMethodPatches.Prefixes) methods[i].Item2.Add(new HarmonyPatch(patch.PatchMethod, HarmonyPatchType.Prefix, patch.owner, patch.index, patch.priority));
                    foreach (Patch patch in frameMethodPatches.Postfixes) methods[i].Item2.Add(new HarmonyPatch(patch.PatchMethod, HarmonyPatchType.Postfix, patch.owner, patch.index, patch.priority));
                    foreach (Patch patch in frameMethodPatches.Transpilers) methods[i].Item2.Add(new HarmonyPatch(patch.PatchMethod, HarmonyPatchType.Transpiler, patch.owner, patch.index, patch.priority));
                    foreach (Patch patch in frameMethodPatches.Finalizers) methods[i].Item2.Add(new HarmonyPatch(patch.PatchMethod, HarmonyPatchType.Finalizer, patch.owner, patch.index, patch.priority));
                }
            }
        }

        private MethodInfo GetBaseMeth(MethodInfo info)
        {
            foreach (MethodBase methodBase in Harmony.GetAllPatchedMethods())
            {
                Patches infos = Harmony.GetPatchInfo(methodBase);
                foreach (Patch infosPrefix in infos.Prefixes) if (infosPrefix.PatchMethod == info) return methodBase as MethodInfo;
                foreach (Patch infosPrefix in infos.Postfixes) if (infosPrefix.PatchMethod == info) return methodBase as MethodInfo;
                foreach (Patch infosPrefix in infos.Transpilers) if (infosPrefix.PatchMethod == info) return methodBase as MethodInfo;
                foreach (Patch infosPrefix in infos.Finalizers) if (infosPrefix.PatchMethod == info) return methodBase as MethodInfo;
            }
            return null;
        }

        public int Count { get; set; } = 1;

        // Each method, has a list of the 
        public List<Tuple<MethodInfo, List<HarmonyPatch>>> methods = new List<Tuple<MethodInfo, List<HarmonyPatch>>>();
        public string translatedString = null;
        public string[] translatedStringArr = null;
        public string firstUnique = null;
        public int idx = 0;

        public string TranslatedForm()
        {
            return translatedString;
        }

        public string FirstUnqiue()
        {
            return firstUnique;
        }

        public string[] TranslatedArr()
        {
            return translatedStringArr;
        }
    }


}
