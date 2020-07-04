using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Verse.Noise;

namespace DubsAnalyzer
{
    public static class StackTraceRegex
    {
        public static Dictionary<string, StackTraceInformation> traces = new Dictionary<string, StackTraceInformation>();

        private const string strRegex = @"(?<=:)(DMD.*)(?<=::)|(>)";
        private static Regex myRegex = new Regex(strRegex, RegexOptions.None);

        public static void Add(StackTrace trace)
        {
            var key = trace.ToString();

            if (traces.ContainsKey(key))
            {
                traces[key].Count++;
            }
            else
            {
                traces.Add(key, new StackTraceInformation(trace));
                IdentifyFirstUnique();
            }
        }

        public static void IdentifyFirstUnique()
        {
            List<StackTraceInformation> things = traces.Values.ToList();

            for(int i = 0; i < things.Max(w => w.methods.Count); i++)
            {
                for(int j = 0; j < things.Count; j++)
                {
                    for(int h = 0; h < things.Count; h++)
                    {
                        if(things[j].methods.ElementAt(i).Key != things[h].methods.ElementAt(i).Key)
                        {
                            foreach(var e in things)
                            {
                                e.firstUnique = e.methods.ElementAt(i).Key.Name;
                                e.idx = i;
                            }
                            return;
                        }
                    }
                }
            }
        }

        public static void Reset()
        {
            traces = new Dictionary<string, StackTraceInformation>();
        }

        private static string QuickKey(StackTrace stackTrace)
        {
            
            StringBuilder builder = new StringBuilder();
            foreach (var frame in stackTrace.GetFrames())
                builder.Append(frame.GetMethod().Name);

            return builder.ToString();
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
            var retStr = Regex.Replace(str, "#", "\n");
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
            MethodInfo patch;
            HarmonyPatchType type;
            string id;
            int index;
            int priority;
        }

        public StackTraceInformation(StackTrace input)
        {
            ProccessInput(input);
        }

        private void ProccessInput(StackTrace stackTrace)
        {
            // Translate our input into the strings we will want to show the user
            var rawString = StackTraceRegex.StackTrace(stackTrace);
            var processedString = StackTraceRegex.ProcessString(rawString);

            translatedString = processedString;
            translatedStringArr = processedString.Split('\n');

            // Lets get the relevant methods that will be required for interactivity
            for(int i = 0; i < stackTrace.FrameCount; i++)
            {
                var frame = stackTrace.GetFrame(i);
                MethodInfo frameMethod = frame.GetMethod() as MethodInfo;

                var frameMethodPatches = Harmony.GetPatchInfo(frameMethod);
                methods.Add(frameMethod as MethodInfo, new List<HarmonyPatch>());
                if(frameMethodPatches != null) // add relevant patch information
                {
                    foreach (var patch in frameMethodPatches.Prefixes)      methods[frameMethod].Add(new HarmonyPatch(patch.PatchMethod, HarmonyPatchType.Prefix,       patch.owner, patch.index, patch.priority));
                    foreach (var patch in frameMethodPatches.Postfixes)     methods[frameMethod].Add(new HarmonyPatch(patch.PatchMethod, HarmonyPatchType.Postfix,      patch.owner, patch.index, patch.priority));
                    foreach (var patch in frameMethodPatches.Transpilers)   methods[frameMethod].Add(new HarmonyPatch(patch.PatchMethod, HarmonyPatchType.Transpiler,   patch.owner, patch.index, patch.priority));
                    foreach (var patch in frameMethodPatches.Finalizers)    methods[frameMethod].Add(new HarmonyPatch(patch.PatchMethod, HarmonyPatchType.Finalizer,    patch.owner, patch.index, patch.priority));
                }
            }
        }

        public int Count { get; set; } = 1;

        // Each method, has a list of the 
        public Dictionary<MethodInfo, List<HarmonyPatch>> methods = new Dictionary<MethodInfo, List<HarmonyPatch>>();
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
