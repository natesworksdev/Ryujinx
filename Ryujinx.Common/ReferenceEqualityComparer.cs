using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Ryujinx.Common
{
    public readonly struct ReferenceEqualityComparer<T> : IEqualityComparer<T>
        where T : class
    {
        public readonly bool Equals(T x, T y) => x == y;

        public readonly int GetHashCode([DisallowNull] T obj) => obj.GetHashCode();
    }
}
