using Ryujinx.Graphics.GAL;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.OpenGL
{
    static class Handle
    {
        public static T FromUInt32<T>(uint handle) where T : unmanaged
        {
            Debug.Assert(Unsafe.SizeOf<T>() == sizeof(ulong));

            ulong handle64 = handle;

            return Unsafe.As<ulong, T>(ref handle64);
        }

        public static uint ToUInt32(this BufferHandle handle)
        {
            return (uint)Unsafe.As<BufferHandle, ulong>(ref handle);
        }
    }
}
