using Xunit;

// Profiler.cs imports these namespaces but uses no types from them.
namespace Verse { }
namespace UnityEngine.Assertions { }

namespace Analyzer.Profiling
{
    // Replaces the RimWorld-dependent logger. Profiler.cs calls it in DEBUG builds only.
    internal static class ThreadSafeLogger
    {
        public static void Error(string message) => Assert.Fail(message);
    }
}
