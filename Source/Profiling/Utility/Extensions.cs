using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Analyzer.Profiling
{
    public static class ListExtensions
    {
        public static void InPlaceConcat<T>(this IEnumerable<T> instance, params IEnumerable<T>[] lists)
        {
            foreach (var list in lists)
                foreach (var inst in list)
                    instance.Append(inst);
        }
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> instance, params IEnumerable<T>[] lists)
        {
            foreach (var list in lists)
                foreach (var inst in list)
                    yield return inst;

            foreach (var inst in instance)
                yield return inst;
        }

        public static void AdjustHorizonallyBy(this ref Rect rect, int width)
        {
            rect.x += width;
            rect.width -= width;
        }

        public static void AdjustHorizonallyBy(this ref Rect rect, float width)
        {
            rect.x += width;
            rect.width -= width;
        }

        public static Rect RetAdjustHorizonallyBy(this Rect rect, int width)
        {
            rect.x += width;
            rect.width -= width;

            return rect;
        }

        public static Rect RetAdjustHorizonallyBy(this Rect rect, float width)
        {
            rect.x += width;
            rect.width -= width;

            return rect;
        }

        public static void AdjustVerticallyBy(this ref Rect rect, int height)
        {
            rect.y += height;
            rect.height -= height;
        }

        public static void AdjustVerticallyBy(this ref Rect rect, float height)
        {
            rect.y += height;
            rect.height -= height;
        }

        public static Rect RetAdjustVerticallyBy(this Rect rect, int height)
        {
            rect.y += height;
            rect.height -= height;

            return rect;
        }

        public static Rect RetAdjustVerticallyBy(this Rect rect, float height)
        {
            rect.y += height;
            rect.height -= height;

            return rect;
        }
    }

}
