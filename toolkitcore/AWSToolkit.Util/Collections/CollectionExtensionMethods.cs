using System.Collections;
using System.Collections.Generic;

namespace Amazon.AWSToolkit.Collections
{
    /// <summary>
    /// Provides extended functionality to collection objects.
    /// </summary>
    /// <remarks>
    /// Be aware that the implementation-specific collection type that these methods are
    /// applied to may affect the results.  For example, lists will maintain order while
    /// other collection types may not.  Sets will throw an exception on duplicate additions
    /// where other collection types may not.
    /// </remarks>
    public static class CollectionExtensionMethods
    {
        public static void AddAll<T>(this ICollection<T> @this, IEnumerable<T> items)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                @this.Add(item);
            }
        }

        public static void AddAll(this IList @this, IEnumerable items)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                @this.Add(item);
            }
        }

        public static bool RemoveAll<T>(this ICollection<T> @this, IEnumerable<T> items)
        {
            if (items == null)
            {
                return true;
            }

            bool removedAll = true;

            foreach (var item in items)
            {
                removedAll &= @this.Remove(item);
            }

            return removedAll;
        }

        public static void RemoveAll(this IList @this, IEnumerable items)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                @this.Remove(item);
            }
        }
    }
}
