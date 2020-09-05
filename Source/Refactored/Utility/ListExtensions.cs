using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer
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
    }
}
