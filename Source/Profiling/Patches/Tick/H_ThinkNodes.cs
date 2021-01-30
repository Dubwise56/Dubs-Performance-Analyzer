using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace Analyzer.Profiling
{
	//[Entry("entry.tick.thinknodes", Category.Tick)]
	//internal static class H_ThinkNodes
	//{
	//    public static bool Active = false;
	//    public static List<MethodInfo> patched = new List<MethodInfo>();

	//    public static IEnumerable<MethodInfo> GetPatchMethods()
	//    {
	//        foreach (Type typ in GenTypes.AllTypes)
	//        {
	//            if (typeof(ThinkNode_JobGiver).IsAssignableFrom(typ))
	//            {
	//                MethodInfo trygive = AccessTools.Method(typ, nameof(ThinkNode_JobGiver.TryGiveJob));
	//                if (!trygive.DeclaringType.IsAbstract && trygive.DeclaringType == typ)
	//                {
	//                    if (!patched.Contains(trygive))
	//                    {
	//                        yield return trygive;
	//                        patched.Add(trygive);
	//                    }
	//                }
	//            }
	//            else if (typeof(ThinkNode).IsAssignableFrom(typ))
	//            {
	//                MethodInfo mef = AccessTools.Method(typ, nameof(ThinkNode.TryIssueJobPackage));

	//                if (!mef.DeclaringType.IsAbstract && mef.DeclaringType == typ)
	//                {
	//                    if (!patched.Contains(mef))
	//                    {
	//                        yield return mef;
	//                        patched.Add(mef);
	//                    }
	//                }
	//            }
	//        }
	//    }
	//}


	[StaticConstructorOnStartup]
	internal class H_ThinkNodes
	{
		public static bool Active = false;

		public static List<MethodInfo> patched = new List<MethodInfo>();

		public static int NodeIndex = 0;

		public static Entry p = Entry.Create("entry.tick.thinknodes", Category.Tick, typeof(H_ThinkNodes), false);

		public static void ProfilePatch()
		{
			var go = new HarmonyMethod(typeof(H_ThinkNodes), nameof(Start));
			var biff = new HarmonyMethod(typeof(H_ThinkNodes), nameof(Stop));

			void slop(Type e, string s)
			{
				Modbase.Harmony.Patch(AccessTools.Method(e, s), go, biff);
			}

			foreach (var typ in GenTypes.AllTypes)
			{
				if (typeof(ThinkNode_JobGiver).IsAssignableFrom(typ))
				{
					var trygive = AccessTools.Method(typ, nameof(ThinkNode_JobGiver.TryGiveJob));
					if (!typ.IsAbstract && trygive.DeclaringType == typ)
					{
						if (!patched.Contains(trygive))
						{
							slop(typ, nameof(ThinkNode_JobGiver.TryGiveJob));

							patched.Add(trygive);
						}
					}
				}
				else if (typeof(ThinkNode_Tagger).IsAssignableFrom(typ))
				{
					var mef = AccessTools.Method(typ, nameof(ThinkNode_Tagger.TryIssueJobPackage));

					if (!typ.IsAbstract && mef.DeclaringType == typ)
					{
						if (!patched.Contains(mef))
						{
							slop(typ, nameof(ThinkNode_Tagger.TryIssueJobPackage));

							patched.Add(mef);
						}
					}
				}
				else if (typeof(ThinkNode).IsAssignableFrom(typ))
				{
					var mef = AccessTools.Method(typ, nameof(ThinkNode.TryIssueJobPackage));

					if (!typ.IsAbstract && mef.DeclaringType == typ)
					{
						if (!patched.Contains(mef))
						{
							slop(typ, nameof(ThinkNode.TryIssueJobPackage));

							patched.Add(mef);
						}
					}
				}
			}
		}

		[HarmonyPriority(Priority.Last)]
		public static void Start(ThinkNode __instance, MethodBase __originalMethod, ref Profiler __state)
		{
			if (p.isActive)
			{
				__state = p.Start(__originalMethod.DeclaringType.Name, __originalMethod);
			}
		}

		[HarmonyPriority(Priority.First)]
		public static void Stop(Profiler __state)
		{
			if (p.isActive)
			{
				__state?.Stop();
			}
		}
	}
}