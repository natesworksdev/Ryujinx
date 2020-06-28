using Ryujinx.Common.Memory;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Nvdec.Vp9.Common
{
    internal static class MemoryUtil
    {
        private static unsafe ref T AllocateInternal<T>(int length = 1) where T : unmanaged
        {
            IntPtr ptr = Marshal.AllocHGlobal(Unsafe.SizeOf<T>() * length);

            return ref Unsafe.AsRef<T>((T*)ptr);
        }

        public static unsafe ArrayPtr<T> Allocate<T>(int length) where T : unmanaged
        {
            return new ArrayPtr<T>(ref AllocateInternal<T>(length), length);
        }

        public static unsafe void Free<T>(ArrayPtr<T> arr) where T : unmanaged
        {
            Marshal.FreeHGlobal((IntPtr)arr.ToPointer());
        }

        public static unsafe void Copy<T>(T* dest, T* source, int length) where T : unmanaged
        {
            new Span<T>(source, length).CopyTo(new Span<T>(dest, length));
        }

        public static unsafe void Copy<T>(ref T dest, ref T source) where T : unmanaged
        {
            MemoryMarshal.CreateSpan(ref source, 1).CopyTo(MemoryMarshal.CreateSpan(ref dest, 1));
        }

        public static unsafe void Fill<T>(T* ptr, T value, int length) where T : unmanaged
        {
            new Span<T>(ptr, length).Fill(value);
        }
    }
}
