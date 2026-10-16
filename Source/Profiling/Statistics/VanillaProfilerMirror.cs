#if V1_7
using System;
using System.Collections.Generic;
using Waylith.Profiling;

namespace Analyzer.Profiling
{
    public static class VanillaProfilerMirror
    {
        public static bool enabled;
        private static bool used;

        [ThreadStatic] private static List<(Profiler profiler, bool entered)> open;

        public static bool Active => enabled && FrameProfiler.IsCapturing;
        public static bool Used => used;

        public static void Enter(Profiler profiler)
        {
            if ((object)profiler.key == H_RootUpdate.FrameTimeKey) return;

            if (profiler.vanillaLabel < 0)
                profiler.vanillaLabel = FrameProfiler.GetOrCreateLabelId("PA: " + profiler.label).Value;

            open ??= new List<(Profiler, bool)>();
            open.Add((profiler, FrameProfiler.TryEnterScope(new ProfilerLabelId((ushort)profiler.vanillaLabel))));
            used = true;
        }

        public static void Leave(Profiler profiler)
        {
            var list = open;
            if (list == null) return;

            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].profiler != profiler) continue;
                Close(list, i);
                return;
            }
        }

        public static void EndFrame()
        {
            if (used && open is { Count: > 0 }) Close(open, 0);
        }

        private static void Close(List<(Profiler profiler, bool entered)> list, int index)
        {
            for (var i = list.Count - 1; i >= index; i--)
                if (list[i].entered) FrameProfiler.LeaveScope();
            list.RemoveRange(index, list.Count - index);
        }
    }
}
#endif
