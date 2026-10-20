using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    public class Panel_StackTraces : IBottomTabRow
    {
        public static bool currentlyTracking = false;
        public static int currentTrackedStacktraces => StackTraceUtility.captured;

        private const int CaptureStep = 128, CaptureCap = 2048;
        private const float RowHeight = 18f;

        private static Profiler tracedProfiler;
        private static Type filterType;
        private static long postfixCycle = -1;
        private static uint postfixSlot;
        private static int cycleCalls, cycleCaptures, rate = 1;

        private readonly MethodInfo postfix = typeof(Panel_StackTraces).GetMethod(nameof(StacktracePostfix), BindingFlags.Public | BindingFlags.Static);
        private readonly MethodInfo postfixInstance = typeof(Panel_StackTraces).GetMethod(nameof(StacktracePostfixInstance), BindingFlags.Public | BindingFlags.Static);
        private MethodInfo currentMethod;

        private int stripCount = 300, stripOffset;
        private long selectedTrace, selectedCycle = -1;
        private float stripHeight = 80f, dragStartX, dragStartOffset;
        private bool draggingSplitter, draggingStrip, dragMoved;

        private int selectedDistance
        {
            get => selectedCycle < 0 ? 0 : (int)(ProfileController.cycle - selectedCycle);
            set => selectedCycle = value <= 0 ? -1 : ProfileController.cycle - value;
        }
        private Vector2 tableScroll, stackScroll;

        public void ResetState(GeneralInformation? info)
        {
            currentlyTracking = false;
            tracedProfiler = null;
            selectedDistance = 0;
            selectedTrace = 0;
            currentMethod = null;
            tableScroll = stackScroll = Vector2.zero;

            if (info == null) return;

            StackTraceUtility.Reset();
            Modbase.Harmony.CreateProcessor(info.Value.method).Unpatch(postfix);
            Modbase.Harmony.CreateProcessor(info.Value.method).Unpatch(postfixInstance);
        }

        public void Draw(Rect rect, GeneralInformation? info)
        {
            if (info == null || info.Value.method == null) return;
            currentMethod = info.Value.method as MethodInfo;

            var font = Text.Font;
            Text.Font = GameFont.Tiny;
            Text.WordWrap = false;
            rect = rect.ContractedBy(2f);
            if (selectedDistance >= Profiler.RECORDS_HELD || selectedDistance > Analyzer.GetCurrentLogCount) selectedDistance = 0;

            try
            {

            DrawToolbar(rect.TopPartPixels(22f), currentMethod);
            rect.AdjustVerticallyBy(24f);

            stripHeight = Mathf.Clamp(stripHeight, 30f, Mathf.Max(30f, rect.height - 80f));
            var strip = rect.TopPartPixels(stripHeight);
            DrawSamples(strip);
            DrawSplitter(new Rect(rect.x, strip.yMax, rect.width, 8f));
            rect.AdjustVerticallyBy(strip.height + 8f);

            var table = rect.LeftPartPixels(rect.width * 0.55f);
            DrawTable(table);
            DrawStack(rect with { xMin = table.xMax + 6f });
            }
            finally
            {
                Text.Font = font;
                Text.WordWrap = true;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
        }

        private void DrawToolbar(Rect rect, MethodInfo method)
        {
            var tracking = currentlyTracking;
            var toggle = rect.LeftPartPixels(Text.CalcSize(Strings.stack_trace).x + 34f);
            Widgets.CheckboxLabeled(toggle, Strings.stack_trace, ref tracking, placeCheckboxNearText: true);
            TooltipHandler.TipRegion(toggle, Strings.stack_trace_tip);
            if (tracking != currentlyTracking) SetTracking(method, tracking);
            rect.AdjustHorizonallyBy(toggle.width + 10f);

            Text.Anchor = TextAnchor.MiddleLeft;
            var status = currentlyTracking
                ? Strings.stack_status_tracing(Utility.GetSignature(method, false), StackTraceUtility.captured.ToString("N0"), StackTraceUtility.traces.Count.ToString("N0"))
                : StackTraceUtility.captured > 0 ? Strings.stack_status_stopped(StackTraceUtility.captured.ToString("N0"), StackTraceUtility.traces.Count.ToString("N0")) : Strings.stack_status_idle;
            Widgets.Label(rect, status.Truncate(rect.width));
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void SetTracking(MethodInfo method, bool on)
        {
            Modbase.Harmony.CreateProcessor(method).Unpatch(postfix);
            Modbase.Harmony.CreateProcessor(method).Unpatch(postfixInstance);
            tracedProfiler = null;

            if (on)
            {
                StackTraceUtility.Reset();
                selectedDistance = 0;
                selectedTrace = 0;
                postfixCycle = -1;
                tracedProfiler = GUIController.CurrentProfiler;
                var declaring = method.DeclaringType;
                var type = tracedProfiler?.type;
                filterType = !method.IsStatic && type != null && declaring != null && type != declaring && declaring.IsAssignableFrom(type) ? type : null;
                Modbase.Harmony.Patch(method, postfix: new HarmonyMethod(method.IsStatic ? postfix : postfixInstance));
            }

            currentlyTracking = on;
        }

        private static string Units => GUIController.CurrentCategory == Category.Tick ? Strings.unit_ticks : Strings.unit_frames;

        private static StackTraceUtility.SlotTraces SlotAt(Profiler profiler, int distance, out int slot)
        {
            slot = 0;
            if (distance < 1 || distance >= Profiler.RECORDS_HELD) return null;
            slot = (int)((profiler.currentIndex + Profiler.RECORDS_HELD - distance) % Profiler.RECORDS_HELD);
            var traces = StackTraceUtility.slots[slot];
            return traces != null && traces.cycle == ProfileController.cycle - distance ? traces : null;
        }

        private void DrawSplitter(Rect rect)
        {
            var line = new Rect(rect.x, rect.center.y - 1f, rect.width, 2f);
            Widgets.DrawBoxSolid(line, Mouse.IsOver(rect) || draggingSplitter ? new Color(1f, 1f, 1f, 0.5f) : new Color(1f, 1f, 1f, 0.15f));
            TooltipHandler.TipRegion(rect, Strings.stack_splitter_tip);

            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && Mouse.IsOver(rect))
            {
                draggingSplitter = true;
                e.Use();
            }
            else if (draggingSplitter && e.type == EventType.MouseDrag)
            {
                stripHeight += e.delta.y;
                e.Use();
            }
            else if (draggingSplitter && e.rawType == EventType.MouseUp)
            {
                draggingSplitter = false;
            }
        }

        private void DrawSamples(Rect rect)
        {
            var profiler = GUIController.CurrentProfiler;
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f));
            if (profiler == null) return;

            var logCount = Analyzer.GetCurrentLogCount;
            var count = Mathf.Min(stripCount, logCount);
            if (count <= 0) return;
            stripOffset = Mathf.Clamp(stripOffset, 0, Mathf.Max(0, logCount - count));

            var width = rect.width / count;
            var e = Event.current;
            var mouseFromRight = rect.xMax - e.mousePosition.x;

            if (Mouse.IsOver(rect) && e.type == EventType.ScrollWheel)
            {
                var fromRight = Mathf.Clamp01(mouseFromRight / rect.width);
                var anchor = stripOffset + fromRight * count;
                stripCount = Mathf.Clamp(stripCount + (int)(e.delta.y * stripCount * 0.07f), 20, Profiler.RECORDS_HELD);
                count = Mathf.Min(stripCount, logCount);
                stripOffset = Mathf.Clamp(Mathf.RoundToInt(anchor - fromRight * count), 0, Mathf.Max(0, logCount - count));
                width = rect.width / count;
                e.Use();
            }

            if (e.type == EventType.MouseDown && e.button == 0 && Mouse.IsOver(rect))
            {
                draggingStrip = true;
                dragMoved = false;
                dragStartX = e.mousePosition.x;
                dragStartOffset = stripOffset;
                e.Use();
            }
            else if (draggingStrip && e.type == EventType.MouseDrag)
            {
                if (Mathf.Abs(e.mousePosition.x - dragStartX) > 3f) dragMoved = true;
                if (dragMoved) stripOffset = Mathf.Clamp(Mathf.RoundToInt(dragStartOffset + (e.mousePosition.x - dragStartX) / width), 0, Mathf.Max(0, logCount - count));
                e.Use();
            }
            else if (draggingStrip && e.rawType == EventType.MouseUp)
            {
                draggingStrip = false;
                if (!dragMoved && Mouse.IsOver(rect))
                {
                    var d = stripOffset + 1 + Mathf.FloorToInt(mouseFromRight / width);
                    selectedDistance = selectedDistance == d ? 0 : d;
                    selectedTrace = 0;
                    tableScroll = Vector2.zero;
                }
            }

            var max = 0.001;
            for (var d = stripOffset + 1; d <= stripOffset + count; d++) max = Math.Max(max, profiler.times[(profiler.currentIndex + Profiler.RECORDS_HELD - d) % Profiler.RECORDS_HELD]);

            var selected = selectedDistance;
            for (var i = 1; i <= count; i++)
            {
                var d = stripOffset + i;
                var traces = SlotAt(profiler, d, out var slot);
                var time = profiler.times[slot];
                var x = rect.xMax - i * width;
                var column = new Rect(x, rect.y, width, rect.height);
                var height = (float)(time / max) * (rect.height - 2f);
                var bar = new Rect(x, rect.yMax - height, Mathf.Max(1f, width - (width > 3f ? 1f : 0f)), height);

                Widgets.DrawBoxSolid(bar, d == selected ? Color.white : traces != null ? new Color(0.4f, 0.65f, 0.95f) : new Color(0.35f, 0.35f, 0.35f));
                if (d == selected) Widgets.DrawBoxSolid(column with { y = rect.yMax - 2f, height = 2f }, Color.white);

                if (!Mouse.IsOver(column)) continue;

                Widgets.DrawBoxSolid(column, new Color(1f, 1f, 1f, 0.08f));
                TooltipHandler.TipRegion(column, new TipSignal(() => Strings.stack_sample_tip(d.ToString(), Units, profiler.times[slot].ToString("0.000"), profiler.hits[slot].ToString("N0")) + "\n" + (SlotAt(profiler, d, out _) == null ? Strings.stack_sample_none : Strings.stack_sample_click), "PA.StackSamples".GetHashCode()));
            }

            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(rect.ContractedBy(3f, 1f), $"{max:0.00} ms");
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(rect.ContractedBy(3f, 1f), selected == 0 ? Strings.stack_samples_hint : Strings.stack_samples_selected(selected.ToString()));
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }
        private List<(long key, StackTraceInformation info, float calls)> Rows(out string title)
        {
            var rows = new List<(long, StackTraceInformation, float)>();
            var profiler = GUIController.CurrentProfiler;

            lock (StackTraceUtility.sync)
            {
                var slot = profiler == null || selectedDistance == 0 ? null : SlotAt(profiler, selectedDistance, out _);
                if (slot != null)
                {
                    var scale = slot.calls > slot.weight && slot.weight > 0 ? slot.calls / slot.weight : 1f;
                    foreach (var (key, weight) in slot.counts)
                        if (StackTraceUtility.traces.TryGetValue(key, out var info)) rows.Add((key, info, weight * scale));
                    title = Strings.stack_title_sample(selectedDistance.ToString(), Units, slot.calls.ToString("N0")) + (filterType != null ? " " + Strings.stack_title_ontype(filterType.Name) : "") + (slot.sampled ? ", " + Strings.stack_title_sampled : "");
                }
                else
                {
                    foreach (var (key, info) in StackTraceUtility.traces) rows.Add((key, info, info.Weight));
                    title = selectedDistance == 0 ? (filterType != null ? Strings.stack_title_alltype(filterType.Name) : Strings.stack_title_all) : Strings.stack_title_none;
                    if (selectedDistance != 0) rows.Clear();
                }
            }

            rows.Sort((a, b) => b.Item3.CompareTo(a.Item3));
            return rows;
        }

        private void DrawTable(Rect rect)
        {
            var rows = Rows(out var title);
            var total = rows.Sum(r => r.calls);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(rect.TopPartPixels(RowHeight), title);
            rect.AdjustVerticallyBy(RowHeight);

            var header = rect.TopPartPixels(RowHeight);
            var callsW = 70f;
            var pctW = 50f;
            var patchW = 50f;
            GUI.color = new Color(0.65f, 0.65f, 0.65f);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(header.LeftPartPixels(callsW), Strings.stack_col_calls);
            Widgets.Label(new Rect(header.x + callsW, header.y, pctW, RowHeight), "%");
            Widgets.Label(new Rect(header.xMax - patchW - 16f, header.y, patchW, RowHeight), Strings.stack_col_patches);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(header.x + callsW + pctW + 8f, header.y, header.width - callsW - pctW - patchW - 24f, RowHeight), Strings.stack_col_calledfrom);
            GUI.color = Color.white;
            TooltipHandler.TipRegion(header, Strings.stack_table_tip);
            rect.AdjustVerticallyBy(RowHeight);

            Widgets.DrawLineHorizontal(rect.x, rect.y, rect.width);

            var view = new Rect(0f, 0f, rect.width - 16f, rows.Count * RowHeight);
            Widgets.BeginScrollView(rect, ref tableScroll, view, true);

            for (var i = 0; i < rows.Count; i++)
            {
                var row = new Rect(0f, i * RowHeight, view.width, RowHeight);
                if (row.yMax < tableScroll.y || row.y > tableScroll.y + rect.height) continue;

                var (key, info, calls) = rows[i];
                if (key == selectedTrace) Widgets.DrawHighlightSelected(row);
                Widgets.DrawHighlightIfMouseover(row);

                var caller = info.Methods.FirstOrDefault(m => m.method != currentMethod);
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(row.LeftPartPixels(callsW), $"{Math.Round(calls):N0}");
                Widgets.Label(new Rect(row.x + callsW, row.y, pctW, RowHeight), total > 0 ? $"{calls / total:P0}" : "");
                Widgets.Label(new Rect(row.xMax - patchW, row.y, patchW, RowHeight), info.ModsInvolved.ToString());
                Text.Anchor = TextAnchor.MiddleLeft;
                var name = caller == null ? "?" : $"{caller.MethodString}  ({caller.Mod})";
                Widgets.Label(new Rect(row.x + callsW + pctW + 8f, row.y, row.width - callsW - pctW - patchW - 12f, RowHeight), name.Truncate(row.width - callsW - pctW - patchW - 12f));

                if (Widgets.ButtonInvisible(row))
                {
                    selectedTrace = key;
                    stackScroll = Vector2.zero;
                }
            }

            Widgets.EndScrollView();
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawStack(Rect rect)
        {
            StackTraceInformation info;
            lock (StackTraceUtility.sync) StackTraceUtility.traces.TryGetValue(selectedTrace, out info);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(rect.TopPartPixels(RowHeight), info == null ? Strings.stack_select : Strings.stack_full);
            Text.Anchor = TextAnchor.UpperLeft;
            rect.AdjustVerticallyBy(RowHeight);

            if (info != null) DrawStackTrace(rect, info);
        }

        private void DrawStackTrace(Rect rect, StackTraceInformation stackTrace)
        {
            var methods = stackTrace.Methods.ToList();
            var modWidth = Mathf.Min(140f, rect.width * 0.25f);
            var patchWidth = 50f;
            var methodWidth = rect.width - modWidth - patchWidth - 16f;

            GUI.color = new Color(0.65f, 0.65f, 0.65f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(rect.x, rect.y, methodWidth, RowHeight), Strings.stack_col_method);
            Widgets.Label(new Rect(rect.x + methodWidth, rect.y, modWidth, RowHeight), Strings.stack_col_mod);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(rect.x + methodWidth + modWidth, rect.y, patchWidth, RowHeight), Strings.stack_col_patches);
            GUI.color = Color.white;
            rect.AdjustVerticallyBy(RowHeight);
            Widgets.DrawLineHorizontal(rect.x, rect.y, rect.width);

            var view = new Rect(0f, 0f, rect.width - 16f, methods.Count * RowHeight);
            Widgets.BeginScrollView(rect, ref stackScroll, view, true);

            for (var i = 0; i < methods.Count; i++)
            {
                var trace = methods[i];
                var y = i * RowHeight;
                var row = new Rect(0f, y, view.width, RowHeight);

                Widgets.DrawHighlightIfMouseover(row);
                if (Widgets.ButtonInvisible(row) && trace.method != null)
                    Find.WindowStack.Add(new FloatMenu(MethodMenu(trace).ToList()));

                Text.Anchor = TextAnchor.MiddleLeft;
                DrawStringWithin(new Rect(0f, y, methodWidth, RowHeight), trace.MethodString, true);
                DrawStringWithin(new Rect(methodWidth, y, modWidth, RowHeight), trace.Mod, false);

                var patchRect = new Rect(methodWidth + modWidth, y, patchWidth, RowHeight);
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(patchRect, trace.Patches.ToString());
                if (trace.Patches > 0) TooltipHandler.TipRegion(patchRect, trace.SummaryString);
            }

            Widgets.EndScrollView();
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static IEnumerable<FloatMenuOption> MethodMenu(MethodMeta trace)
        {
            var method = trace.method;
            var category = GUIController.CurrentCategory == Category.Tick ? Category.Tick : Category.Update;
            var name = $"{method.DeclaringType?.FullName}:{method.Name}";
            var categoryName = category == Category.Tick ? Strings.tab_tick : Strings.tab_update;

            yield return new FloatMenuOption(Strings.stack_menu_profile(method.Name, categoryName), () => Panel_DevOptions.ExecutePatch(CurrentInput.Method, name, category));

            if (method is MethodInfo info)
                yield return new FloatMenuOption(Strings.stack_menu_internal(method.Name, categoryName), () => Utility.PatchInternalMethod(info, category));

            if (trace.Patches > 0)
                yield return new FloatMenuOption(Strings.stack_menu_patches(method.Name, categoryName), () => Panel_DevOptions.ExecutePatch(CurrentInput.MethodHarmony, name, category));

            yield return new FloatMenuOption(Strings.stack_menu_copy, () =>
            {
                var te = new TextEditor { text = Utility.GetSignature(method, false) };
                te.SelectAll();
                te.Copy();
            });

            yield return new FloatMenuOption(Strings.stack_menu_dnspy, () => Panel_BottomRow.OpenDnspy(method));
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

        public static void StacktracePostfix() => Capture(null);

        public static void StacktracePostfixInstance(object __instance) => Capture(__instance);

        private static void Capture(object instance)
        {
            var profiler = tracedProfiler;
            if (profiler == null || Analyzer.CurrentlyPaused) return;
            if (filterType != null && instance?.GetType() != filterType) return;

            var cycle = ProfileController.cycle;
            if (cycle != postfixCycle)
            {
                if (postfixCycle >= 0) StackTraceUtility.SetCalls(postfixSlot, postfixCycle, cycleCalls);
                postfixCycle = cycle;
                postfixSlot = profiler.currentIndex;
                cycleCalls = cycleCaptures = 0;
                rate = 1;
            }

            if (++cycleCalls % rate != 0 || cycleCaptures >= CaptureCap) return;

            var weight = rate;
            if (++cycleCaptures % CaptureStep == 0) rate *= 2;

            StackTraceUtility.Add(new StackTrace(1, false), weight, postfixSlot, cycle, cycleCalls);
        }
    }
}
