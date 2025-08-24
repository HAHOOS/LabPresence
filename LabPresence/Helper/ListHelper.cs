using System;
using System.Collections.Generic;

namespace LabPresence.Helper
{
    /// <summary>
    /// A helper class for <see cref="List{T}"/>
    /// </summary>
    public static class ListHelper
    {
        /// <summary>
        /// Remove the first item that meets the criteria set by <paramref name="predicate"/>
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="List{T}"/></typeparam>
        /// <param name="list">The <see cref="List{T}"/> to remove the first item from</param>
        /// <param name="predicate">The criteria that the item needs to met</param>
        /// <returns>Was an item removed</returns>
        public static bool RemoveFirst<T>(this List<T> list, Predicate<T> predicate)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (predicate.Invoke(list[i]))
                {
                    list.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
    }
}