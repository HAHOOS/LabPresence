using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabPresence.Helper
{
    public static class ListHelper
    {
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