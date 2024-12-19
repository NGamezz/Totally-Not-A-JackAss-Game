using System;
using System.Collections.Generic;

public static class EnumerableExtensions
{
    public static IEnumerable<T> WhereType<T, TParam>(
        this IEnumerable<T> source,
        TParam param,
        Func<T, TParam, bool> predicate)
    {
        foreach (var item in source)
        {
            if (predicate(item, param))
                yield return item;
        }
    }
}