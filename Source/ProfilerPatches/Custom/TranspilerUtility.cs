using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DubsAnalyzer
{
    public struct TargetCode
    {
        List<CodeInstruction> instructionsToMatch;
        List<int> occurences;
    }

    public class TranspilerUtility
    {
        public static void ProfileMethod(MethodInfo method, Func<string> namer, params TargetCode[] targets)
        {

        }
    }
}
