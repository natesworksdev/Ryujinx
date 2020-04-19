using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ryujinx.Common.Extensions
{
    public static class CollectionExtensions
    {
        public static ReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this Dictionary<TKey, TValue> collection) =>
            new ReadOnlyDictionary<TKey, TValue>(collection);

        public static ReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> collection) =>
            new ReadOnlyDictionary<TKey, TValue>(collection);
    }
}
