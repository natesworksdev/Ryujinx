using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ARMeilleure.Common
{
    internal static class BitMapPool
    {
        [MethodImpl(MethodOptions.FastInline)]
        [return: NotNull]
        internal static BitMap Allocate(int initialCapacity)
        {
            BitMap result = ThreadStaticPool<BitMap>.Instance.Allocate();
            result.Reset(initialCapacity);
            return result;
        }

        [MethodImpl(MethodOptions.FastInline)]
        internal static void Release()
        {
            ThreadStaticPool<BitMap>.Instance.Clear();
        }
    }
}
