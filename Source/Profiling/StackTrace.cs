using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Analyzer.Profiling
{
    public static class StackTraceRegex
    {
        public static Dictionary<string, StackTraceInformation> traces = new Dictionary<string, StackTraceInformation>();

        // todo, this was removed in some ver of harmony - double check
        private const string strRegex = @"(?<=:)(DMD.*)(?<=::)|(>)"; // Get rid of the garbled error messages that harmony patched methods create   
        private static readonly Regex myRegex = new Regex(strRegex, RegexOptions.None);

        public static void Add(StackTrace trace)
        {
            var key = trace.ToString();

            if(traces.TryGetValue(key, out var value))  value.Count++;
            else traces.Add(key, new StackTraceInformation(trace));
        }

        public static void Reset()
        {
            traces = new Dictionary<string, StackTraceInformation>();
        }

        public static string ExtractStackTraceInformation(StackTrace stackTrace)
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

        public static string ProcessString(string str)
        {
            string retStr = myRegex.Replace(str, @"");

            return retStr;
        }

    }

    public class StackTraceInformation
    {
        public int Count { get; set; } = 1;
        public List<Tuple<MethodInfo, List<Patch>>> methods = new List<Tuple<MethodInfo, List<Patch>>>();
        private string[] translatedStringArr = null;
        public string[] TranslatedArr() => translatedStringArr;

        public StackTraceInformation(StackTrace input) => ProcessInput(input);

        private void ProcessInput(StackTrace stackTrace)
        {
            // Translate our input into the strings we will want to show the user
            var processedString = StackTraceRegex.ProcessString(StackTraceRegex.ExtractStackTraceInformation(stackTrace));

            translatedStringArr = processedString.Split('#');

            // Get patch methods for any methods in the stack trace
            for (int i = 0; i < stackTrace.FrameCount; i++)
            {
                var frameMethod = stackTrace.GetFrame(i).GetMethod();
                var baseMeth = GetBaseMeth(frameMethod as MethodInfo);
                var patches = Harmony.GetPatchInfo(baseMeth);

                methods.Insert(i, new Tuple<MethodInfo, List<Patch>>(baseMeth, new List<Patch>()));
                if (patches == null) continue;

                foreach (var patch in patches.Prefixes.Concat(patches.Postfixes, patches.Transpilers, patches.Transpilers )) 
                    methods[i].Item2.Add(patch);
            }
        }

        private static MethodInfo GetBaseMeth(MethodInfo info)
        {
            foreach (var m in Harmony.GetAllPatchedMethods())
            {
                var infos = Harmony.GetPatchInfo(m);

                if (infos.Prefixes.Concat(infos.Postfixes, infos.Transpilers, infos.Transpilers ).Any(patch => patch.PatchMethod == info))
                    return m as MethodInfo;
            }
            return null;
        }
    }


}
