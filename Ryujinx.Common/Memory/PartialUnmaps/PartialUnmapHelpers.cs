using System.Runtime.CompilerServices;

namespace Ryujinx.Common.Memory.PartialUnmaps
{
    static class PartialUnmapHelpers
    {
        public static int OffsetOf<T, T2>(ref T2 storage, ref T target)
        {
            return (int)Unsafe.ByteOffset(ref Unsafe.As<T2, T>(ref storage), ref target);
        }
    }
}
