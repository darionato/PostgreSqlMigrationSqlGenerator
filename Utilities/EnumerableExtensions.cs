using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Badlydone.Utilities
{
    public static class EnumerableExtensions
    {

        public static void Each<T>(this IEnumerable<T> ts, Action<T, int> action)
        {
            Contract.Requires(ts != null);
            Contract.Requires(action != null);

            var i = 0;
            foreach (var t in ts)
            {
                action(t, i++);
            }
        }

        public static void Each<T>(this IEnumerable<T> ts, Action<T> action)
        {
            Contract.Requires(ts != null);
            Contract.Requires(action != null);

            foreach (var t in ts)
            {
                action(t);
            }
        }

        public static void Each<T, TS>(this IEnumerable<T> ts, Func<T, TS> action)
        {
            Contract.Requires(ts != null);
            Contract.Requires(action != null);

            foreach (var t in ts)
            {
                action(t);
            }
        }

        public static string Join<T>(this IEnumerable<T> ts, Func<T, string> selector = null, string separator = ", ")
        {
            Contract.Requires(ts != null);

            selector = selector ?? (t => t.ToString());

            return string.Join(separator, ts.Where(t => !ReferenceEquals(t, null)).Select(selector));
        }

    }
}
