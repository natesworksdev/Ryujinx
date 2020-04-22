using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ARMeilleure.Common
{
    static class BitMapPool
    {
        [MethodImpl(MethodOptions.FastInline)]
        [return: NotNull]
        public static BitMap Allocate(int initialCapacity)
        {
            BitMap result = ThreadStaticPool<BitMap>.Instance.Allocate();
            result.Reset(initialCapacity);
            return result;
        }

        [MethodImpl(MethodOptions.FastInline)]
        public static void Release()
        {
            ThreadStaticPool<BitMap>.Instance.Clear();
        }
    }
}
