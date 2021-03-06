﻿using System.Collections.Generic;
using System.Linq;

namespace Photosphere.SearchEngine.Utils.Extensions
{
    internal static class EnumerableExtensions
    {
        public static IEnumerable<T> NotNull<T>(this IEnumerable<T> e) => e.Where(i => i != null);
    }
}