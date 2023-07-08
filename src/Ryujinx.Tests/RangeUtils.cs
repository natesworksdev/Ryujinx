using System.Collections.Generic;
using System.Numerics;

namespace Ryujinx.Tests
{
    public static class RangeUtils
    {
        public static IEnumerable<T> RangeData<T>(T from, T to, T step) where T : INumber<T>
        {
            List<T> data = new();

            for (T i = from; i < to; i += step)
            {
                data.Add(i);
            }

            return data.AsReadOnly();
        }
    }
}
