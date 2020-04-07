using System;
using System.Collections.Generic;
using System.Linq;

namespace Amazon.AWSToolkit.Util
{
    public static class ListExtensionMethods
    { 
        /// <summary>
        /// Breaks a list into a list of smaller lists, preserving item order
        /// </summary>
        /// <param name="list">List to be split</param>
        /// <param name="maxChunkSize">Max size of lists to be created</param>
        /// <returns>A list of lists created from <param name="list"></param></returns>
        public static IList<List<T>> Split<T>(this IList<T> list, int maxChunkSize)
        {
            var chunks = new List<List<T>>();

            if (list == null)
            {
                return chunks;
            }

            if (maxChunkSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxChunkSize));
            }

            var startPos = 0;
            while (startPos < list.Count)
            {
                chunks.Add(list.Skip(startPos).Take(maxChunkSize).ToList());
                startPos += maxChunkSize;
            }

            return chunks;
        }
    }
}