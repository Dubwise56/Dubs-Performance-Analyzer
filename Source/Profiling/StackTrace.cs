using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using HarmonyLib;
using UnityEngine;
using Verse;
using Object = System.Object;

namespace Analyzer.Profiling
{
    // Adapted from https://github.com/pardeike/VisualExceptions/blob/main/Source/Patcher.cs#L145-L159
    static class RememberHarmonyIDs
    {
        private static Assembly harmonyAssembly = typeof(Harmony).Assembly;
        private static Assembly dpaAssembly = typeof(Modbase).Assembly;
        
        internal static void Prefix(string id)
        {
            try
            {
                if (Utility.IsNotAnalyzerPatch(id) is false) return;

                // Can't be sure that the JIT won't inline the constructor of harmony
                // so we need to manually trawl until we find a method which doesn't belong
                // to either dpa or harmony. (this prefix, and the harmony ctor)
                var frames = new StackTrace(false).GetFrames();
                if (frames == null) return;

                var methods = frames.Select(Harmony.GetMethodFromStackframe).Select(m => m is MethodInfo mi ? Harmony.GetOriginalMethod(mi) ?? m : m);
                
                // find the first non harmony/dpa method in the stack trace and assoc the asm
                // that method is from with the stack trace
                foreach (var method in methods)
                {
                    var asm = method.DeclaringType?.Assembly ?? method.ReflectedType?.Assembly;                    
                    if (asm == null) continue;
                    
                    if (asm.FullName.Contains("Harmony") || asm == dpaAssembly) continue;
                    
                    // we snipe this in another way
                    if (method.Name == "ApplyHarmonyPatches") return;
                    
                    StackTraceUtility.RegisterHarmonyId(id, asm);
                    return;
                }

            }
            catch (Exception e) // lets be exceedingly careful.
            {
                ThreadSafeLogger.ReportException(e, "Failed to capture Harmony ctor");
            }
        }
        
        // patches Hugslib.ModBase:ApplyHarmonyPatches
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var harmonyMethod = AccessTools.Method("HarmonyLib.Harmony:PatchAll", new Type[]{ typeof(Assembly) } );
            
            foreach (var inst in instructions)
            {
                yield return inst;
                if (inst.Calls(harmonyMethod))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Object), nameof(Object.GetType)));
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Type), "get_Assembly"));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StackTraceUtility), nameof(StackTraceUtility.RegisterHarmonyId)));
                }
            }
        }
        
        
    }
    
    public class MethodMeta
    {
        public MethodMeta(MethodBase m, HarmonyLib.Patches p)
        {
            this.method = m;
            this.patches = p;
            this.methodStr = Utility.GetSignature(method);

            if (p != null)
            {
                this.summaryStr = StackTraceUtility.SummaryString(p);
                this.patchCount = p.Owners.Where(Utility.IsNotAnalyzerPatch).Count();
            }
            else
            {
                this.summaryStr = "";
                this.patchCount = 0;
            }

            var foundMod = StackTraceUtility.mods.TryGetValue(m.DeclaringType.Assembly, out mod);
            if (foundMod) return;
            
            // We add Assembly-CSharp into the `mods` dict, so all we
            // need to check is the UnityEngine components.
            if (!foundMod && m.DeclaringType.Assembly.FullName.Contains("UnityEngine"))
                this.mod = "Rimworld";
            else
                this.mod = "Unknown";
        }
        
        public MethodBase method;
        public HarmonyLib.Patches patches;

        private string summaryStr;
        private string methodStr;
        private string mod;
        private int patchCount;

        public string Mod => mod;
        
        public int Patches => patchCount;
        public string MethodString => methodStr;
        public string SummaryString => summaryStr;
    }
    public static class StackTraceUtility
    {
        public static Dictionary<long, StackTraceInformation> traces = new Dictionary<long, StackTraceInformation>();
        internal static Dictionary<MethodBase, string> cachedStrings = new Dictionary<MethodBase, string>();
        internal static Dictionary<string, Assembly> harmonyIds = new Dictionary<string, Assembly>();
        internal static Dictionary<string, StackTrace> harmonyIds_d = new Dictionary<string, StackTrace>();

        internal static readonly Dictionary<Assembly, string> mods = new Dictionary<Assembly, string>();

        private static FieldInfo visualExceptionsHarmonyIds = null;

        public static void Initialise()
        {
            try {
                foreach (var mod in LoadedModManager.RunningMods)
                {
                    foreach (var asm in mod.assemblies.loadedAssemblies)
                    {
                        if(mods.ContainsKey(asm) == false)
                            mods.Add(asm, mod.Name);
                    }
                }
            
                var assembly = typeof(Modbase).Assembly;
            
                // Manually add vanilla to our mod list
                var vanillaAsm = typeof(Pawn).Assembly;
                if (mods.ContainsKey(vanillaAsm)) {
                    var modName = mods[vanillaAsm];
                    ThreadSafeLogger.Error($"Mod {modName} has packaged the base-game Rimworld assemblies");
                } else {
                    mods.Add(vanillaAsm, "Rimworld");
                }

                // Manually add our harmony ids.
                harmonyIds.Add(Modbase.Harmony.Id, assembly);
                harmonyIds.Add(Modbase.StaticHarmony.Id, assembly);
            }
            catch (Exception e) {
                foreach(var (key, val) in mods)  {
                    ThreadSafeLogger.Error("Caught an error when building an assembly -> mod mapping, dumping existing mapping");
                    ThreadSafeLogger.Message($"Asm: {key}, maps to mod {val}");
                }

                throw e;
            }
        }

        public static void Add(StackTrace trace)
        {
            long seed = 0x9e3779b9;
            
            foreach (var m in trace.GetFrames().Select(s => s.GetMethod()))
            {
                seed ^= m.GetHashCode() + 0x9e3779b9 + (seed << 6) + (seed >> 2);
            }
            

            if(traces.TryGetValue(seed, out var value)) value.Count++;
            else traces.Add(seed, new StackTraceInformation(trace));
        }

        public static void Reset()
        {
            cachedStrings.Clear();
            traces.Clear();
        }

        public static void RegisterHarmonyId(string harmonyid, Assembly assembly)
        {
            if (harmonyid == null || assembly == null) return;

            try {
                if (harmonyIds.TryGetValue(harmonyid, out var asm))
                {
                    mods.TryGetValue(asm, out var asmMod);
                    mods.TryGetValue(assembly, out var assemblyMod);

                    // Doesn't matter a whole lot
                    if (asmMod == assemblyMod)
                    {
                        return;
                    }
                    
                    var blameString = $"This is a Harmony ID configuration issue with {asmMod} and {assemblyMod}. Please report that they are instantiating harmony with the same ID.";
                    
                    ThreadSafeLogger.Message($"The HarmonyID {harmonyid} has been loaded twice. It is associated with both: \n{asm.FullName} and {assembly.FullName}\n{blameString}\n\nEnable verbose logging and restart to view the associated callstack(s)");
                    if (Settings.verboseLogging)
                    {
                        ThreadSafeLogger.Message($"Initial Trace: {GetStackTraceString(harmonyIds_d[harmonyid], out _)}");
                        ThreadSafeLogger.Message($"Current Trace: {GetStackTraceString(new StackTrace(1, false), out _)}");       
                    }
                }
                else
                {
                    harmonyIds.Add(harmonyid, assembly);
                    if (Settings.verboseLogging)
                    {
                        harmonyIds_d.Add(harmonyid, new StackTrace(1, false));
                    }
                }
            } catch(Exception e) {
                ThreadSafeLogger.ReportException(e, "Caught error when intercepting harmony initialization");
            }
        }
        
        public static string ModFromPatchId(string pid)
        {
            if (Modbase.visualExceptionIntegration)
            {
                visualExceptionsHarmonyIds ??= AccessTools.Field(AccessTools.TypeByName("VisualExceptions.Mods"), "ActiveHarmonyIDs");

                if (visualExceptionsHarmonyIds.GetValue(null) is Dictionary<Assembly, string> dict && harmonyIds.Count != dict.Count)
                {
                    harmonyIds.Clear();
                    harmonyIds = dict.ToDictionary(pair => pair.Value, pair => pair.Key);
                }
            }
            
            if (harmonyIds.TryGetValue(pid, out var asm) && mods.TryGetValue(asm, out var modName))
                return modName;

            return pid;
        }
        
        public static string SummaryString(HarmonyLib.Patches patch)
        {
            var sb = new StringBuilder(255);

            void ProcessPatches(ReadOnlyCollection<Patch> patches, string type)
            {
                if (patches.Any() is false) return;
                
                sb.Append($"{type}: {{ ");
                
                var first = true;
                
                foreach (var p in patches.Where(p => Utility.IsNotAnalyzerPatch(p.owner)).OrderBy(s => s.priority))
                {
                    sb.Append((first ? "" : ", ") + $"{ModFromPatchId(p.owner)}");
                    if(type == "Prefixes") sb.Append((p.PatchMethod.ReturnType == typeof(bool) ? " - destructive" : null));
                    
                    first = false;
                }

                sb.Append(" } ");
            }

            ProcessPatches(patch.Prefixes, "Prefixes");
            ProcessPatches(patch.Postfixes, "Postfixes");
            ProcessPatches(patch.Transpilers, "Transpilers");
            ProcessPatches(patch.Finalizers, "Finalizers");

            return sb.ToString();
        }
        private static string GetPatchStrings(MethodBase method, HarmonyLib.Patches p)
        {
            const string spacePrefix = "      - "; // 6 spaces

            var retString = new StringBuilder();
            retString.Append("\n" + spacePrefix);
            retString.Append(SummaryString(p));
            
            return retString.ToString();
        }
        
        public static string GetStackTraceString(StackTrace st, out List<MethodMeta> methods)
        {
            var stringBuilder = new StringBuilder(255);
            methods = new List<MethodMeta>();

            for (int i = 0; i < st.FrameCount; i++)
            {
                var method = Harmony.GetMethodFromStackframe(st.GetFrame(i));

                if (method is MethodInfo replacement)
                {
                    var original = Harmony.GetOriginalMethod(replacement);
                    if (original != null)
                        method = original;
                }

                GetStringForMethod(ref stringBuilder, method);

                if (method != null)
                {
                    var p = Harmony.GetPatchInfo(method);

                    if (p != null && p.Owners.Count != 0)
                    {
                        var pString = GetPatchStrings(method, p);

                        if (pString != null)
                            stringBuilder.Append(pString);
                    }

                    methods.Add(new MethodMeta(method, p));
                }
                else
                    methods.Add(null);
                
                stringBuilder.Append("\n");
            }

            return stringBuilder.ToString();
        }
        
        private static void GetStringForMethod(ref StringBuilder sb, MethodBase meth)
        {
            if (meth == null) return;

            if (cachedStrings.TryGetValue(meth, out var result) is false)
            {
                result = Utility.GetSignature(meth);
                cachedStrings.Add(meth, result);
            }
            
            sb.Append(result);
        }
        
    }

    public class StackTraceInformation
    {
        private List<MethodMeta> methods;
        
        public int Count { get; set; } = 1;
        public int Depth { get; private set; } = 0;
        public string Header { get; set; } = "";
        public int ModsInvolved { get; private set; } = 0;

        public int LongestMethod { get; private set; } = 0;
        public int LongestSummary { get; private set; } = 0;

        public MethodMeta Method(int depth) => methods[depth];
        public IEnumerable<MethodMeta> Methods => methods;

        public StackTraceInformation(StackTrace input) => ProcessInput(input);

        private void ProcessInput(StackTrace stackTrace)
        {
            // Translate our input into the strings we will want to show the user
            _ = StackTraceUtility.GetStackTraceString(stackTrace, out methods);

            Depth = methods.Count - 1;

            LongestMethod = methods.Max(t => (int)t.MethodString.GetWidthCached());
            LongestSummary = methods.Max(t => (int)t.SummaryString.GetWidthCached());
            ModsInvolved = methods.Sum(m => m.Patches);
        }
    }


}
