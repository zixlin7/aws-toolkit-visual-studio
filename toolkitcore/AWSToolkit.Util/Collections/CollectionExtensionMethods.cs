using System.Collections.Generic;

namespace Amazon.AWSToolkit.Collections
{
    public static class CollectionExtensionMethods
    {
        public static void AddAll<T>(this ICollection<T> @this, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                @this.Add(item);
            }
        }
    }
}
