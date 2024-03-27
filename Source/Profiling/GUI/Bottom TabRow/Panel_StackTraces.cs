using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    public class Panel_StackTraces : IBottomTabRow
    {
        // called from the postfix which is applied which captures the stack trace, hence why it is static
        public static bool currentlyTracking = false;
        public static int currentTrackedStacktraces = 0;
        
        private const int GOAL_TRACKED_TRACES = 100_000;
        private long currentTrace = 0;
        private MethodInfo currentMethod = null;
        private MethodInfo postfix = typeof(Panel_StackTraces).GetMethod(nameof(StacktracePostfix), BindingFlags.Public | BindingFlags.Static);
        private StackTraceInformation CurrentTrace => (currentTrace == 0) ? null : StackTraceUtility.traces[currentTrace];
        private Vector2 scrollPosition = Vector2.zero;

        public void ResetState(GeneralInformation? info)
        {
            currentlyTracking = false;
            currentTrace = 0;
            currentTrackedStacktraces = 0;
            currentMethod = null;
            scrollPosition = Vector2.zero;

            if (info == null) return; 
            
            StackTraceUtility.Reset();
            Modbase.Harmony.CreateProcessor(info.Value.method).Unpatch(postfix);
        }

        public void Draw(Rect rect, GeneralInformation? info)
        {
            if (info == null || info.Value.method == null) return;
            currentMethod = info.Value.method as MethodInfo;
            
            var panelWidth = Mathf.Clamp(rect.width / 4, 0, 65);
            var leftHandPanel = rect.LeftPartPixels(panelWidth);
            rect.AdjustHorizonallyBy(panelWidth + 4f);
            DrawLeftColumn(leftHandPanel, currentMethod);
            
            var statusBox = rect.TopPartPixels(30f);
            rect.AdjustVerticallyBy(30f);
            DrawCurrentStatus(statusBox, currentMethod);

            if (currentTrace == 0)
            {
                if(StackTraceUtility.traces.Count > 0)
                    currentTrace = StackTraceUtility.traces.MaxBy(t => t.Value.Count).Key;
            } else
            {
                DrawStackTrace(rect, StackTraceUtility.traces[currentTrace]);
            } 
        }

        // Vertical strip along the top of the UI informing the user of the current 
        // status of the patching
        private void DrawCurrentStatus(Rect statusBox, MethodInfo method)
        {
            const string activeTemplate = "Tracing; {0:#,##0.##} traces collected";
            const string idleTemplate = "Idle";
            const string inactiveTemplate = "Finished; {0:#,##0.##} traces collected";
            const string traceInformationTempate = "Calls: {0:#,##0.##} ({1:P}), Mods Involved {2}  ";
            
            var status = "";

            if (currentlyTracking)
                status = string.Format(activeTemplate, currentTrackedStacktraces);
            else
            {
                status = currentTrackedStacktraces != 0 
                    ? string.Format(inactiveTemplate, currentTrackedStacktraces) 
                    : string.Format(idleTemplate);
            }

            var statusWidth = status.GetWidthCached();
            var tracingStatusRect = statusBox.LeftPartPixels(statusWidth);
            statusBox.AdjustHorizonallyBy(statusWidth);
            Widgets.Label(tracingStatusRect, status);

            var ct = CurrentTrace;
            if (ct == null) return;

            var modsInvolved = CurrentTrace.Methods.Sum(st => st.Patches);
            
            status = string.Format(traceInformationTempate, ct.Count, ct.Count / (float)currentTrackedStacktraces, modsInvolved);
            Widgets.Label(statusBox.RightPartPixels(status.GetWidthCached()), status);
        }

        // Horizontal column which offers the option to
        // Enable, Disable, View Different Stacktrace, Summary
        public void DrawLeftColumn(Rect rect, MethodInfo method)
        {
            var col = GUI.color;
            GUI.color = col * new Color(1f, 1f, 1f, 0.4f);
            Widgets.DrawLineVertical(rect.x + rect.width, rect.y, rect.height);
            GUI.color = col;
            
            var ts = Text.Font;
            Text.Font = GameFont.Small;
            
            var height = rect.height - 12;
            var individualHeight = height / 3f;
            var methodString = Utility.GetSignature(method, false);
            
            // Enable
            var enableRect = rect.TopPartPixels(individualHeight);
            rect.AdjustVerticallyBy(individualHeight);
            DrawEnableButton(enableRect, method, methodString);

            DrawLine(ref rect);

            // Disable
            var disableRect = rect.TopPartPixels(individualHeight);
            rect.AdjustVerticallyBy(individualHeight);
            DrawDisableButton(disableRect, method);
            
            DrawLine(ref rect);

            // Change
            var changeTraceRect = rect.TopPartPixels(individualHeight);
            rect.AdjustVerticallyBy(individualHeight);
            DrawChangeTraceButton(changeTraceRect);

            Text.Font = ts;
        }
        public void DrawLine(ref Rect rect)
        {
            rect.AdjustVerticallyBy(2);
            
            var col = GUI.color;
            GUI.color = col * new Color(1f, 1f, 1f, 0.4f);
            Widgets.DrawLineHorizontal(rect.x, rect.y, rect.width);
            GUI.color = col;
            
            rect.AdjustVerticallyBy(2);
        }
        private void DrawEnableButton(Rect rect, MethodInfo method, string methodString)
        {
            var tooltip = $"Enable stack trace profiling";
            var height = rect.height;
            if (height > 25f + Text.LineHeight)
            {
                DubGUI.CenterText(() => Widgets.Label(rect.TopPartPixels(Text.LineHeight), "Enable"));
                rect.AdjustVerticallyBy(Text.LineHeight);
            }

            if (currentlyTracking)
                Widgets.DrawHighlightSelected(rect);
            
            var centerRect = rect.CenterWithDimensions(25, 25);
            TooltipHandler.TipRegion(centerRect, tooltip);
            
            if (Widgets.ButtonImage(centerRect, Widgets.CheckboxOnTex))
            {
                if (currentlyTracking is false)
                {
                    StackTraceUtility.Reset();
                    currentTrace = 0;
                    currentTrackedStacktraces = 0;
                    
                    Modbase.Harmony.Patch(method, postfix: new HarmonyMethod(postfix));
                }
                else
                {
                    ThreadSafeLogger.ErrorOnce($"Can not retrace {methodString} while currently tracing", method.GetHashCode());
                }

                currentlyTracking = true;
            }
        }

        private void DrawDisableButton(Rect rect, MethodInfo method)
        {
            var height = rect.height;
            if (height > 25f + Text.LineHeight)
            {
                DubGUI.CenterText(() => Widgets.Label(rect.TopPartPixels(Text.LineHeight), "Disable"));
                rect.AdjustVerticallyBy(Text.LineHeight);
            }

            var tooltip = $"Disable stack trace profiling";

            if (currentlyTracking is false)
                GUI.color = Color.gray;

            var centerRect = rect.CenterWithDimensions(25, 25);
            TooltipHandler.TipRegion(centerRect, tooltip);
            
            if (Widgets.ButtonImage(centerRect, Widgets.CheckboxOffTex))
            {
                if (currentlyTracking)
                {
                    Modbase.Harmony.CreateProcessor(method).Unpatch(postfix);
                }

                currentlyTracking = false;
            }
            
            if (currentlyTracking is false)
                GUI.color = Color.white;
        }

        private void DrawChangeTraceButton(Rect rect)
        {
            var height = rect.height;
            if (height > 25f + Text.LineHeight)
            {
                DubGUI.CenterText(() => Widgets.Label(rect.TopPartPixels(Text.LineHeight), "Change"));
                rect.AdjustVerticallyBy(Text.LineHeight);
            }

            var tooltip = $"Change the currently viewed stacktrace";

            if (currentTrackedStacktraces == 0)
                GUI.color = Color.gray;
            
            var centerRect = rect.CenterWithDimensions(25, 25);
            TooltipHandler.TipRegion(centerRect, tooltip);

            if(Widgets.ButtonImage(centerRect, Textures.Burger))
            {
                if (currentTrackedStacktraces != 0)
                {
                    var traces = StackTraceUtility.traces.Values.ToList();
                    CalculateHeaders(traces, traces.MaxBy(t => t.Depth).Depth);
                    
                    var options = StackTraceUtility.traces
                        .OrderBy(p => p.Value.Count)
                        .Reverse()
                        .Select(st => new FloatMenuOption($"{st.Value.Header} : {st.Value.Count}", () => { currentTrace = st.Key; }))
                        .ToList();

                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }
            
            if (currentTrackedStacktraces == 0)
                GUI.color = Color.white;
        }
        private void CalculateHeaders(List<StackTraceInformation> stackTraces, int maxDepth)
        {
            // Find first unique method at a given depth
            // from each callstack and display it 
            
            //  2      another     |      different    |      blahblah
            //  1      notunique   |     notunique     |     uniquepath
            //  0      target      |       target      |      target
            //       ----------------------------------------------------
            //Header:  another     |     different     |     uniquepath
            
            for (var depth = 0; depth < maxDepth; depth++)
            {
                var seenMethods = new Dictionary<MethodBase, int>();
                for (var i = 0; i < stackTraces.Count; i++)
                {
                    var stackTrace = stackTraces[i];
                    if (stackTrace.Depth < depth || stackTrace.Header.NullOrEmpty() is false) continue;

                    var method = stackTrace.Method(depth);
                    if (stackTrace.Depth == depth)
                    {
                        stackTrace.Header = Utility.GetSignature(stackTrace.Method(depth).method, false);
                    }

                    if (seenMethods.TryGetValue(method.method, out var idx))
                    {
                        var otherTrace = stackTraces[idx];
                        
                        if(otherTrace.Depth > depth)
                            otherTrace.Header = "";
                    }
                    else
                    {
                        seenMethods.Add(stackTrace.Method(depth).method, i);
                        stackTrace.Header = Utility.GetSignature(method.method, false);
                    }
                }
            }
        }

        private void DrawStackTrace(Rect rect, StackTraceInformation stackTrace)
        {
            const string dummyLen = "XXXXXXXXXXXXXXXX"; // 16 chars length
            const string dummyAddition = "XXXX";

            var dummyAdditionLen = dummyAddition.GetWidthCached();
            var textSize = Text.LineHeight;
            
            var patchesLen = "Patches   ".GetWidthCached();
            var modNameLen = Mathf.Min(dummyLen.GetWidthCached(), stackTrace.Methods.MaxBy(t => t.Mod.GetWidthCached()).Mod.GetWidthCached() + dummyAdditionLen);
            var availModLen = rect.width - (patchesLen + modNameLen);
            
            var methodLen = Mathf.Min(availModLen, stackTrace.Methods.MaxBy(t => t.MethodString.GetWidthCached()).MethodString.GetWidthCached() + dummyAdditionLen);
            

            Rect GetMethodColumn(float y, float height) => new Rect(rect.x, y, methodLen, height);
            Rect GetModColumn(float y, float height) => new Rect(rect.x + methodLen, y, modNameLen, height);
            Rect GetPatchColumn(float y, float height) => new Rect(rect.x + methodLen + modNameLen, y, patchesLen, height);

            Widgets.Label( GetMethodColumn(rect.y, textSize), " Method ");
            var anchor = Text.Anchor;
            
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label( GetModColumn(rect.y, textSize), " Mod ");
            Widgets.Label( GetPatchColumn(rect.y, textSize), " Patches ");
            Text.Anchor = anchor;

            rect.AdjustVerticallyBy(textSize);
            
            var col = GUI.color;
            GUI.color = col * new Color(1f, 1f, 1f, 0.4f);
            Widgets.DrawLineHorizontal(rect.x, rect.y, rect.width);
            Widgets.DrawLineVertical(rect.x + methodLen, rect.y - textSize, rect.height + textSize);
            Widgets.DrawLineVertical(rect.x + methodLen + modNameLen + 1, rect.y - textSize, rect.height + textSize);
            GUI.color = col;

            var inner = new Rect(rect.x, 0, rect.width - 17f, stackTrace.Methods.Count() * textSize);
            Widgets.BeginScrollView(rect, ref scrollPosition, inner, false);

            for (int i = 0; i < stackTrace.Methods.Count(); i++)
            {
                var trace = stackTrace.Method(i);

                var methodRect = GetMethodColumn(inner.y + (i * textSize), textSize);
                DrawStringWithin(methodRect, trace.MethodString, true);

                Text.Anchor = TextAnchor.MiddleCenter;
                
                var modRect = GetModColumn(inner.y + (i * textSize), textSize);
                DrawStringWithin(modRect, trace.Mod, true);
                
                var patchRect = GetPatchColumn(inner.y + (i * textSize), textSize);
                Widgets.Label(patchRect, trace.Patches.ToString());
                TooltipHandler.TipRegion(patchRect, trace.SummaryString);

                Text.Anchor = anchor;
            }
            
            Widgets.EndScrollView();
        }

        // Squishes a string of varying size into the rect bounding, if
        // the string is larger than the rect, a tooltip will be drawn 
        // when the rect is hovered over, and the string will be trimmed
        // trimming occurs from the left if the `left` param is true.
        private void DrawStringWithin(Rect bounding, string value, bool left)
        {
            const int MAGIC = 5;
            value = value.Insert(left ? value.Length: 0, "  ");

            var strWidth = value.GetWidthCached();
            var strLen = value.Length;
            
            if (strWidth > bounding.width)
            {
                var diff = strWidth - bounding.width;
                var ratio = diff / (strWidth+MAGIC);

                var charDiff = (int)Math.Ceiling( (strLen + MAGIC) * ratio) ;
                var newLen = (strLen) - charDiff;
                var str = ClipToSize(value, charDiff, newLen, left);
                Widgets.Label(bounding, str);
                TooltipHandler.TipRegion(bounding, value);
            }
            else
            {
                Widgets.Label(bounding, value);
            }
        }

        // Intelligently trims a method string (params first)
        private string ClipToSize(string str, int charsToRemove, int newLen, bool left)
        {
            // No bracket, its not a method
            var lhsParams = str.FirstIndexOf(c => c == '(');
            if (lhsParams == -1) return str.Substring(left ? charsToRemove : 0, newLen);
            
            // How many characters between the parens
            var paramChars = str.Length - lhsParams;
            
            // Enough to warrant chopping it off ( replace it with (...) )
            if (paramChars > 5)
            {
                str = str.Substring(0, lhsParams) + "(...)";
                charsToRemove -= paramChars - 5;
            }

            // Trim the rest of the string (if required)
            if (charsToRemove > 0)
            {
                str = str.Insert(left ? 0 : str.Length, "...");
                str = str.Substring(left ? charsToRemove : 0, Mathf.Min(newLen, str.Length));
            }

            return str;
        }

        // This will be added as a Postfix to the method which we want to gather stack trace information for
        // it will only effect one method, so we can skip the check, and it will not slow down other profilers
        // because it will only be patched onto one method. There can be extra checks and flexibility in how
        // many frames are grabbed p/s etc. These are to be done when the GUI decisions have been made.

        public static void StacktracePostfix()
        {
            // The JIT doesn't seem to inline dynamically generated methods. So it should be safe to hardcode
            // the '2' skipframes, maybe in future updates this will not hold true and should be updated.

            if (currentTrackedStacktraces < GOAL_TRACKED_TRACES)
            {
                StackTraceUtility.Add(new StackTrace(2, false));
                currentTrackedStacktraces++;
            }
            else currentlyTracking = false;
        }

    }
}
