#if V1_7
using System;
using System.Collections.Generic;
using UnityEngine;
using Waylith.Profiling;

namespace Analyzer.Profiling
{
    public static class FrameBreakdown
    {
        public const int Window = 300;

        public static readonly List<(string label, double ms)> parts = new();
        public static double frameMs, workMs;
        public static int samples;

        private static bool ours;
        private static float nextRead;
        private static readonly float[] frameTimes = new float[Window];
        private static int frameIndex, frameCount;
        private static readonly Dictionary<string, double> sums = new();

        public static void Update()
        {
            if (!Analyzer.currentlyProfiling)
            {
                if (ours) FrameProfiler.SetEnabled(false);
                ours = false;
                frameCount = samples = 0;
                parts.Clear();
                return;
            }

            if (Analyzer.CurrentlyPaused) return;

            if (!FrameProfiler.Enabled)
            {
                FrameProfiler.SetEnabled(true);
                ours = true;
            }

            frameTimes[frameIndex] = Time.unscaledDeltaTime * 1000f;
            frameIndex = (frameIndex + 1) % Window;
            frameCount = Math.Min(frameCount + 1, Window);

            if (Time.realtimeSinceStartup < nextRead) return;
            nextRead = Time.realtimeSinceStartup + 0.5f;
            Read();
        }

        private static void Read()
        {
            sums.Clear();
            var entries = ProfilerPathTable.Entries;
            var read = 0;
            var work = 0d;

            using (FrameProfiler.BeginRead())
            {
                for (var i = 0; i < Math.Min(FrameProfiler.HistoryCount, frameCount); i++)
                {
                    if (!FrameProfiler.TryGetHistorySnapshot(i, out var snap) || snap is not { valid: true }) continue;
                    read++;
                    work += snap.totalMs;

                    for (var s = 0; s < snap.sectionCount; s++)
                    {
                        var section = snap.sections[s];
                        if (FrameProfiler.GetLabel(section.nameLabelId) != "MainThread") continue;

                        for (var p = section.firstPathEntry; p < section.firstPathEntry + section.pathEntryCount; p++)
                        {
                            var entry = snap.pathEntries[p];
                            if (entry.pathId <= 0 || entry.pathId >= entries.Length) continue;

                            var parent = entries[entry.pathId].parentPathId;
                            if (parent <= 0 || parent >= entries.Length || entries[parent].parentPathId > 0) continue;

                            var label = FrameProfiler.GetLabel(entries[entry.pathId].labelId);
                            sums.TryGetValue(label, out var ms);
                            sums[label] = ms + entry.totalMicroseconds / 1000d;
                        }
                    }
                }
            }

            parts.Clear();
            samples = read;
            if (read == 0) return;

            var total = 0f;
            for (var i = 0; i < frameCount; i++) total += frameTimes[i];
            frameMs = total / frameCount;
            workMs = work / read;

            var named = 0d;
            foreach (var label in new[] { "PollEvents", "GameUpdate", "CaptureFrame" })
            {
                sums.TryGetValue(label, out var ms);
                parts.Add((label, ms / read));
                named += ms / read;
            }

            parts.Add(("Other engine work", Math.Max(0d, workMs - named)));
            parts.Add(("Waiting", Math.Max(0d, frameMs - workMs)));
        }
    }
}
#endif
