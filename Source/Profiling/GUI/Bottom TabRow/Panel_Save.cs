using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Analyzer.Profiling
{
    class Row
    {
        private static Color[] colours = { Color.red, Color.white, Color.green };
        
        public string name;
        public Func<LogStats, double> getDouble = null;
        public Func<LogStats, int> getInt = null;
        public bool shouldColour;
        
        public Row(string n, Func<LogStats, int> gi, bool sC = true)
        {
            name = n;
            getInt = gi;
            shouldColour = sC;
        }

        public Row(string n, Func<LogStats, double> gd, bool sC = true)
        {
            name = n;
            getDouble = gd;
            shouldColour = sC;
        }
        
        public bool IsInt() => getInt is not null;
        
        public string Get(LogStats stats) {
            return IsInt() ? getInt(stats).ToString() : $"{getDouble(stats):F3}";
        }

        // returns (delta, % diff)
        public (double, double) Delta(LogStats lhs, LogStats rhs) {
            if (IsInt()) {
                var lVal = getInt(lhs);
                var rVal = getInt(rhs);
                return (rVal - lVal, ( (rVal - lVal) / (double) lVal ) * 100);
            } else {
                var lVal = getDouble(lhs);
                var rVal = getDouble(rhs);
                return (rVal - lVal, ( (rVal - lVal) / lVal ) * 100);
            }
        }

        public void DrawDeltaString(Rect rect, LogStats lhs, LogStats rhs) {
            var (delta, perc) = Delta(lhs, rhs);
            var sign = (delta > 0) ? "+" : "";
            
            var color = perc switch
            {
                < -2.5 => colours[2],
                > 2.5  => colours[0],
                _      => colours[1],
            };
            
            GUI.color = color;
            Widgets.Label(rect, $"{sign}{delta:F3} ( {sign}{perc:F2}% )");
            GUI.color = colours[1];
        }

    }

    class SidedEntry {

        protected bool Equals(SidedEntry other) {
            return Equals(file.Name, other.file.Name);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SidedEntry)obj);
        }

        public override int GetHashCode() { 
            unchecked {
                var hashCode = (data != null ? data.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (stats != null ? stats.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (file != null ? file.GetHashCode() : 0);
                return hashCode;
            }
        }

        public EntryFile data;
        public LogStats stats;
        public FileInfo file;

        public SidedEntry(FileInfo file) {
            this.file = file;
            this.data = FileUtility.ReadFile(file);
            this.stats = LogStats.GatherStats(data);
        }

        public static bool operator ==(SidedEntry lhs, SidedEntry rhs) {
            if (ReferenceEquals(lhs, null)) {
                return ReferenceEquals(rhs, null);
            } 
            return !ReferenceEquals(rhs, null) && lhs.Equals(rhs);
        }

        public static bool operator !=(SidedEntry lhs, SidedEntry rhs) {
            return !(lhs == rhs);
        }
    }
    
    public class Panel_Save : IBottomTabRow
    {
        private EntryFile file = null;
        private uint prevIdx = 0;
        private string quantityStr = null;
        private bool flushAtMaxEntries = false;

        private FileHeader curHeader = FileHeader.Default;

        private SidedEntry lhs = null;
        private SidedEntry rhs = null;

        private Vector2 fileScrollPos = Vector2.zero;

        private static List<Row> rows = new List<Row>()
        {
            new Row("Entries", (stats) => stats.Entries, false),
            new Row("Total Calls", (stats) => stats.TotalCalls),
            new Row("Total Time", (stats) => stats.TotalTime),
            new Row("Avg Time/Call", (stats) => stats.MeanTimePerCall),
            new Row("Avg Calls/Update", (stats) => stats.MeanCallsPerUpdateCycle),
            new Row("Avg Time/Update", (stats) => stats.MeanTimePerUpdateCycle),
            new Row("Median Calls", (stats) => stats.MedianCalls),
            new Row("Median Time", (stats) => stats.MedianTime),
            new Row("Max Time", (stats) => stats.HighestTime),
            new Row("Max Calls/Update", (stats) => stats.HighestCalls),
        };

        
        public void ResetState(GeneralInformation? _)
        {
            file = null;
            prevIdx = 0;
            curHeader = FileHeader.Default;

            lhs = null;
            rhs = null;
        }

        private int EntryPerCall(Profiler prof, int idx) {
            var len = prof.hits[prevIdx];
            len = Mathf.Min(file.header.targetEntries - file.header.entries, len);

            if (len == 0) {
                return idx;
            }
            Array.Fill(file.times, prof.times[prevIdx] / prof.hits[prevIdx], idx, len);

            idx += len;
            prevIdx++;

            return idx;
        }

        private int EntriesWithValues(Profiler prof, int idx) {
            if (prof.hits[prevIdx] <= 0) {
                return idx;
            }
            
            file.times[idx] = prof.times[prevIdx];
            file.calls[idx] = prof.hits[prevIdx];

            idx++;
            prevIdx++;
            
            return idx;
        }

        private int DefaultValueImpl(Profiler prof, int idx) {
            var len = (int)(prof.currentIndex >= prevIdx
                ? prof.currentIndex - prevIdx
                : Profiler.RECORDS_HELD - prevIdx);
                    
            len = Math.Min(file.header.targetEntries - file.header.entries, len);
            if (len == 0) {
                return idx;
            };

            Array.Copy(prof.times, (int)prevIdx, file.times, idx, len);
            Array.Copy(prof.hits, (int)prevIdx, file.calls, idx, len);

            idx += len;
            prevIdx += (uint)len;
            
            return idx;
        }
        
        private string GetStatus()
        {
            if (file == null) return "";
        
            var ents = file.header.entries;
            var target = file.header.targetEntries;
            
            return $"{ents}/{target} ({(ents / (float)target) * 100:F2}%)";
        }

        private void UpdateFile(Func<Profiler, int, int> function)
        {
            if (file.header.entries >= file.header.targetEntries) {
                if (flushAtMaxEntries) {
                    Flush();
                }
                return;
            };
            
            var prof = GUIController.CurrentProfiler;
            var idx = file.header.entries;
            
            while (prevIdx != prof.currentIndex && file.header.entries < file.header.targetEntries)
            {
                idx = function(prof, idx);

                prevIdx %= Profiler.RECORDS_HELD;
                file.header.entries = idx;
            }
        }

        public void Draw(Rect r, GeneralInformation? _)
        {
            if (GUIController.CurrentProfiler == null) return;
            
            if (file != null) {
                UpdateFile(file.header.entryPerCall 
                    ? EntryPerCall 
                    : file.header.onlyEntriesWithValues
                        ? EntriesWithValues 
                        : DefaultValueImpl);
            }
            
            var colWidth = Mathf.Min(250, r.width / 3);
            var columnRect = r.RightPartPixels(colWidth);
            r.width -= colWidth;
            
            DrawOptionsColumn(columnRect);
            DrawComparison(r);
        }

        private void Flush() {
            FileUtility.WriteFile(file);
                
            file = null;
            curHeader.entries = 0;
            curHeader.name = "";
        }

        //              [ Left File ]       [ Right File ]     [ Delta ]
        // Calls Mean       25000               23000         -2000 ( -8% )
        // Time Mean       0.037ms             0.031ms        -0.006ms ( - 16% )
        private void DrawComparison(Rect r)
        {
            if (lhs == null || rhs == null) {
                return;
            }
            
            var anchor = Text.Anchor;
            
            var nameColWidth = "  Max Calls/Update".GetWidthCached() + 5f;
            var restWidth = r.width - nameColWidth;
            
            for (int i = 0; i < 4; i++)
            {
                var column = i switch {
                    0 => new Rect(r.x, r.y, nameColWidth, r.height),
                    3 => new Rect(r.x + nameColWidth + restWidth * 4 / 7, r.y, restWidth * 3 / 7, r.height),
                    _ => new Rect((r.x + nameColWidth) + (i - 1) * (restWidth * (2 / 7.0f)), r.y, (restWidth * (2 / 7.0f)), r.height)
                };
                
                Text.Anchor = TextAnchor.MiddleCenter;

                switch (i) {
                    case 0: DubGUI.Heading(column.PopTopPartPixels(30f), "Row");  break;
                    case 1: DubGUI.Heading(column.PopTopPartPixels(30f), "Left");  break;
                    case 2: DubGUI.Heading(column.PopTopPartPixels(30f), "Right");  break;
                    case 3: DubGUI.Heading(column.PopTopPartPixels(30f), "Delta");  break;
                }

                GUI.color = Color.gray;
                Widgets.DrawLineHorizontal(column.x, column.y, column.width);
                GUI.color = Color.white;
                
                Text.Anchor = TextAnchor.MiddleLeft;

                if (i > 0) {
                    Text.Anchor = TextAnchor.MiddleCenter;
                }

                var rect = column.TopPartPixels(Text.LineHeight + 4f);
                int idx = 0;
                foreach (var row in rows)  {
                    if (++idx % 2 == 0) {
                        Widgets.DrawLightHighlight(rect);
                    }
                    
                    switch (i)  {
                        case 0 : Widgets.Label(rect, "  " + row.name); break;
                        case 1 : Widgets.Label(rect, row.Get(lhs.stats)); break;
                        case 2 : Widgets.Label(rect, row.Get(rhs.stats)); break;
                        case 3 : row.DrawDeltaString(rect, lhs.stats, rhs.stats); break;
                    }
                    
                    column.AdjustVerticallyBy(Text.LineHeight + 4f);
                    rect = column.TopPartPixels(Text.LineHeight + 4f);
                }
            }
            Text.Anchor = anchor;
        }

        public void DrawOptionsColumn(Rect r)
        {
            var color = GUI.color;
            GUI.color = color * new Color(1f, 1f, 1f, 0.4f);
            Widgets.DrawLineVertical(r.x, r.y, r.height);
            GUI.color = color;

            r = r.ContractedBy(2);
            
            r.AdjustHorizonallyBy(7f);

            var buttonRowRect = r.PopTopPartPixels(42f);
            var rowPadding = 7f;
            var iconSize = 30f;

            var recordStr = "Record";
            var recordStrLen = recordStr.GetWidthCached();
            var recordButtonRect = buttonRowRect.PopLeftPartPixels(recordStrLen + rowPadding * 2);
            buttonRowRect.AdjustHorizonallyBy(rowPadding);

            var canRecord = file == null && curHeader.targetEntries >= 0;
            if ( ! canRecord )  {
                GUI.color = Color.gray;
            }
            
            if (Widgets.ButtonText(recordButtonRect.CenterWithDimensions(recordStrLen + rowPadding * 2, 30), recordStr) && canRecord) {
                curHeader.methodName = GUIController.CurrentProfiler.label;
                curHeader.entries = 0;
                
                file = new EntryFile()
                {
                    header = curHeader,
                    times = new double[curHeader.targetEntries],
                };
                
                if (!curHeader.entryPerCall)
                    file.calls = new int[curHeader.targetEntries];

                prevIdx = GUIController.CurrentProfiler.currentIndex;
            }
            
            GUI.color = Color.white;

            var pauseButtonRect = buttonRowRect.PopLeftPartPixels(iconSize).CenterWithDimensions(iconSize, 30);
            buttonRowRect.AdjustHorizonallyBy(rowPadding);

            if ( canRecord ) {
                GUI.color = Color.gray;
            }
            
            GUI.DrawTexture(pauseButtonRect, Textures.stoppg);
            if (Widgets.ButtonInvisible(pauseButtonRect) && !canRecord) {
                file = null;
                curHeader.entries = 0;
            }
            
            var saveButtonRect = buttonRowRect.PopLeftPartPixels(iconSize).CenterWithDimensions(iconSize, 30);
            buttonRowRect.AdjustHorizonallyBy(rowPadding);

            GUI.color = Color.white;

            var canSave = file != null && file.header.entries == file.header.targetEntries;
            if (!canSave) {
                GUI.color = Color.gray;
            } 

            GUI.DrawTexture(saveButtonRect, Textures.savebg);
            if (Widgets.ButtonInvisible(saveButtonRect) && canSave) {
                Flush();
            }
            
            Widgets.Label(buttonRowRect, GetStatus());
            
            GUI.color = Widgets.SeparatorLineColor;
            Widgets.DrawLineHorizontal(r.x - 7f, r.y - 2f, r.width + 7f);
            r.AdjVertBy(6f);
            GUI.color = Color.white;

            void DrawOption(string name, ref bool option) {
                var rect = r.PopTopPartPixels(Text.LineHeight);
                
                var checkboxRect = rect.PopRightPartPixels(Text.LineHeight);
                Widgets.DrawTextureFitted(checkboxRect, option ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex, 0.5f);
                if (Widgets.ButtonInvisible(checkboxRect)) {
                    option = !option;
                }
                
                Widgets.Label(rect, name);
            }
            
            // Options
            DrawOption("Only Entries with Values", ref curHeader.onlyEntriesWithValues);
            DrawOption("One Entry per Call", ref curHeader.entryPerCall);
            DrawOption("Auto flush at max entries", ref flushAtMaxEntries);

            // Number of entries
            var numEntriesRect = r.PopTopPartPixels(Text.LineHeight);
            var numEntriesStr = "Entries to record: ";
            var numEntriesStrLen = numEntriesStr.GetWidthCached();
            var numEntriesLabelRect = numEntriesRect.PopLeftPartPixels(numEntriesStrLen + 7f);
            Widgets.Label(numEntriesLabelRect, numEntriesStr);
            numEntriesRect.width -= 7f;
            quantityStr ??= curHeader.targetEntries.ToString();
            Widgets.TextFieldNumeric(numEntriesRect, ref curHeader.targetEntries, ref quantityStr, 0, 10_000);

            GUI.color = Widgets.SeparatorLineColor;
            r.AdjVertBy(6f);
            Widgets.DrawLineHorizontal(r.x - 7f, r.y - 2f, r.width + 7f);
            r.AdjVertBy(6f);
            GUI.color = Color.white;
            
            var fileNameRect = r.PopTopPartPixels(Text.LineHeight);
            var str = "Label: ";
            var strLen = str.GetWidthCached();
            
            Widgets.Label(fileNameRect.PopLeftPartPixels(strLen + 5), str);
            fileNameRect.width -= 7;
            DubGUI.InputField(fileNameRect, "file_name_input", ref curHeader.name);
            
            if (file != null) {
                file.header.name = curHeader.name;
            }
            
            GUI.color = Widgets.SeparatorLineColor;
            Widgets.DrawLineHorizontal(r.x - 7, r.y + 6f, r.width + 7);
            GUI.color = Color.white;
            r.AdjustVerticallyBy(12f);
            
            DrawFileInfo(r);
        }

        private void DrawFileInfo(Rect inRect) {
            
            var prevEntries = FileUtility.PreviousEntriesFor(GUIController.CurrentProfiler.label).ToList();

            var viewRect = inRect;
            viewRect.height = Text.LineHeight * prevEntries.Count;
            if (viewRect.height >= inRect.height) {
                viewRect.width -= 16f;
            }

            Widgets.BeginScrollView(inRect, ref fileScrollPos, viewRect);

            int i = 0;
            foreach (var entry in prevEntries) {
                var rowRect = viewRect.PopTopPartPixels(Text.LineHeight);
                if (++i % 2 == 0) {
                    Widgets.DrawLightHighlight(rowRect);
                }

                if (Widgets.ButtonImage(rowRect.PopRightPartPixels(Text.LineHeight), TexButton.DeleteX)) {
                    ThreadSafeLogger.Message($"Deleting file {entry.info.Name}");
                    FileUtility.DeleteFile(entry.info);
                }

                rowRect.width -= 3.5f;
                GUI.color = Widgets.SeparatorLineColor;
                Widgets.DrawLineVertical(rowRect.x + rowRect.width, rowRect.y, rowRect.height);
                GUI.color = Color.white;
                rowRect.width -= 3.5f;
                
                var rhsAlreadySelected = rhs != null && rhs.file.Name == entry.info.Name;
                if (rhsAlreadySelected) {
                    GUI.color = Color.gray;
                }

                if ( Widgets.ButtonText(rowRect.PopRightPartPixels(Text.LineHeight), "R") && !rhsAlreadySelected) {
                    rhs = new SidedEntry(entry.info);

                    if (lhs == rhs) {
                        lhs = null;
                    }
                }
                
                rowRect.width -= 3.5f;
                GUI.color = Widgets.SeparatorLineColor;
                Widgets.DrawLineVertical(rowRect.x + rowRect.width, rowRect.y, rowRect.height);
                GUI.color = Color.white;
                rowRect.width -= 3.5f;

                var lhsAlreadySelected = lhs != null && lhs.file.Name == entry.info.Name;
                if (lhsAlreadySelected) {
                    GUI.color = Color.gray;
                }
                
                if (Widgets.ButtonText(rowRect.PopRightPartPixels(Text.LineHeight), "L") && !lhsAlreadySelected) {
                    lhs = new SidedEntry(entry.info);

                    if (rhs == lhs) {
                        rhs = null;
                    }
                }
                
                rowRect.width -= 3.5f;
                GUI.color = Widgets.SeparatorLineColor;
                Widgets.DrawLineVertical(rowRect.x + rowRect.width, rowRect.y, rowRect.height);
                GUI.color = Color.white;
                rowRect.width -= 3.5f;

                var name = entry.header.Name;
                if (name == GUIController.CurrentProfiler.label) {
                    // This becomes the integer part of the saved file
                    // I.e. frametime_11 
                    //                ^^
                    name = $"{FileUtility.GetFileNumber(entry.info)} ({entry.info.LastWriteTime:yyyy-M-d hh:mm})";
                }

                Widgets.Label(rowRect, name.Truncate(rowRect.width));
                TooltipHandler.TipRegion(rowRect, entry.info.Name);
            }
            
            Widgets.EndScrollView();
        }
    }
}