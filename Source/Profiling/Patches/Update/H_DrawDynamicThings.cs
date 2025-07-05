using HarmonyLib;
using System;
using System.Reflection;
using Unity.Collections;
using Verse;

namespace Analyzer.Profiling
{
    [Entry("entry.update.dynamicdraw", Category.Update)]
    internal class H_DrawDynamicThings
    {
        public static bool Active = false;

        [Setting("By Def")]
        public static bool ByDef = false;

        public static void ProfilePatch()
        {
            Modbase.Harmony.Patch(AccessTools.Method(typeof(DynamicDrawManager), nameof(DynamicDrawManager.DrawDynamicThings)), prefix: new HarmonyMethod(typeof(H_DrawDynamicThings), "Prefix"));
        }

        public static bool Prefix(MethodBase __originalMethod, DynamicDrawManager __instance)
        {
            if (!Active)
            {
                return true;
            }

          if (!DebugViewSettings.drawThingsDynamic || __instance.map.Disposed)
            return false;

          __instance.drawingNow = true;
          bool flag = SilhouetteUtility.CanHighlightAny();
          var details = new NativeArray<DynamicDrawManager.ThingCullDetails>(__instance.drawThings.Count,
            Allocator.TempJob);
          __instance.ComputeCulledThings(details);
          if (!DebugViewSettings.singleThreadedDrawing)
          {
            using (new ProfilerBlock("Ensure Graphics Initialized"))
            {
              for (int index = 0; index < details.Length; ++index)
              {
                if (details[index].shouldDraw)
                {
                  var thing = __instance.drawThings[index];
                  string Namer()
                    => ByDef
                      ? $"{thing.def.defName} - {thing?.def?.modContentPack?.Name}"
                      : thing.GetType().ToString();

                  var prof = ProfileController.Start(ByDef ? thing.def.defName : thing.GetType().Name, Namer,
                    thing.GetType(), __originalMethod);
                      
                  thing.DynamicDrawPhase(DrawPhase.EnsureInitialized);
                  prof.Stop();
                }
              }
            }

            __instance.PreDrawVisibleThings(details);
          }

          try
          {
            using (new ProfilerBlock("Draw Visible"))
            {
              for (int index = 0; index < details.Length; ++index)
              {
                var cullDetails = details[index];
                if (cullDetails.shouldDraw || cullDetails.shouldDrawShadow)
                {
                  var thing = __instance.drawThings[index];
                  try
                  {
                    if (cullDetails.shouldDraw)
                    {
                      string Namer()
                        => ByDef
                          ? $"{thing.def.defName} - {thing?.def?.modContentPack?.Name}"
                          : thing.GetType().ToString();

                      var prof = ProfileController.Start(ByDef ? thing.def.defName : thing.GetType().Name, Namer,
                        thing.GetType(), __originalMethod);
                      
                      thing.DynamicDrawPhase(DrawPhase.Draw);
                      prof.Stop();
                    }
                    else if (thing is Pawn drawThing)
                      drawThing.DrawShadowAt(drawThing.DrawPos);
                  }
                  catch (Exception ex)
                  {
                    Log.Error(string.Format("Exception drawing {0}: {1}", (object)thing,
                      (object)ex));
                  }
                }
              }
            }

            if (flag)
              __instance.DrawSilhouettes(details);
          }
          catch (Exception ex)
          {
            Log.Error(string.Format("Exception drawing dynamic things: {0}", (object)ex));
          }
          finally
          {
            details.Dispose();
          }

          __instance.drawingNow = false;
          return false;
        }
    }
}