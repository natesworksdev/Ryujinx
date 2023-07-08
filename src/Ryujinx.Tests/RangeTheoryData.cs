using System.Numerics;
using Xunit;

namespace Ryujinx.Tests
{
    public class RangeTheoryData<T> : TheoryData<T> where T : INumber<T>
    {
        public RangeTheoryData(T from, T to, T step)
        {
            for (T i = from; i < to; i += step)
            {
                Add(i);
            }
        }
    }
}
