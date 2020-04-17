using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Ryujinx.Common
{
    public struct ReferenceEqualityComparer<T> : IEqualityComparer<T>
        where T : class
    {
        public bool Equals(T x, T y) => x == y;

        public int GetHashCode([DisallowNull] T obj) => obj.GetHashCode();
    }
}
