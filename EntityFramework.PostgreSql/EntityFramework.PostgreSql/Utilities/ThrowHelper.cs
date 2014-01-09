using System;

namespace EntityFramework.PostgreSql.Utilities
{
    /// <summary>
    /// Extension methods to facilitate throwing exceptions when doing argument checking.
    /// </summary>
    public static class ThrowHelper
    {
        /// <summary>
        /// Throws an ArgumentNullException if the target object is null.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="target">The target object to test against null.</param>
        /// <param name="name">The name of the parameter.</param>
        public static void ThrowIfNull<T>(this T target, string name) where T : class
        {
            if (target == null)
                throw new ArgumentNullException(name);
        }

        /// <summary>
        /// Throws an ArgumentOutOfRangeException if the argument is less than or equal to 0.
        /// </summary>
        /// <param name="argument">The number to test.</param>
        /// <param name="name">The name of the parameter.</param>
        public static void ThrowIfNonPositive(this int argument, string name)
        {
            if (argument <= 0)
                throw new ArgumentOutOfRangeException(name);
        }
    }
}
