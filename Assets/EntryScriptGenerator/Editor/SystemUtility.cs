using System;
using System.Collections.Generic;
using System.Linq;

namespace EntryScriptGenerator.Editor
{
    public static class SystemUtility
    {
        public static bool Exists<T>(this IReadOnlyList<T> list, Predicate<T> predicate)
        {
            return list.Any(item => predicate(item));
        }
    }
}