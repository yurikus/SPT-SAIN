using System;
using System.Collections.Generic;

namespace SAIN.Helpers
{
    internal class ListHelpers
    {
        public static bool ClearCache<T>(List<T> list)
        {
            if (list != null && list.Count > 0)
            {
                list.Clear();
                return true;
            }
            return false;
        }

        public static bool ClearCache<T, V>(Dictionary<T, V> list)
        {
            if (list != null && list.Count > 0)
            {
                list.Clear();
                return true;
            }
            return false;
        }

        public static void PopulateKeys<T, K>(Dictionary<T, K> dictionary, K defaultVal) where T : Enum
        {
            foreach (T item in EnumValues.GetEnum<T>())
            {
                if (dictionary.ContainsKey(item))
                {
                    continue;
                }
                dictionary.Add(item, defaultVal);
            }
        }

        public static void CloneEntries<T, K>(Dictionary<T, K> source, Dictionary<T, K> destination) where T : Enum
        {
            foreach (KeyValuePair<T, K> kvp in source)
            {
                if (destination.ContainsKey(kvp.Key))
                {
                    continue;
                }
                destination.Add(kvp.Key, kvp.Value);
            }
        }
    }
}