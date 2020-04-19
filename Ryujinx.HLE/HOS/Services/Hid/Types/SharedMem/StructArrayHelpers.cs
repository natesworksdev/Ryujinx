using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Hid
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Array2<T> where T : unmanaged
    {
        [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "IntelliSense is unaware of spanned access")]
        T e0, e1;
        public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref e0, 2)[index];
        public readonly int Length => 2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Array3<T> where T : unmanaged
    {
        [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "IntelliSense is unaware of spanned access")]
        T e0, e1, e2;
        public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref e0, 3)[index];
        public readonly int Length => 3;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Array6<T> where T : unmanaged
    {
        [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "IntelliSense is unaware of spanned access")]
        T e0, e1, e2, e3, e4, e5;
        public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref e0, 6)[index];
        public readonly int Length => 6;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Array7<T> where T : unmanaged
    {
        [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "IntelliSense is unaware of spanned access")]
        T e0, e1, e2, e3, e4, e5, e6;
        public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref e0, 7)[index];
        public readonly int Length => 7;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Array10<T> where T : unmanaged
    {
        [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "IntelliSense is unaware of spanned access")]
        T e0, e1, e2, e3, e4, e5, e6, e7, e8, e9;
        public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref e0, 10)[index];
        public readonly int Length => 10;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Array16<T> where T : unmanaged
    {
        [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "IntelliSense is unaware of spanned access")]
        T e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15;
        public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref e0, 16)[index];
        public readonly int Length => 16;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Array17<T> where T : unmanaged
    {
        [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "IntelliSense is unaware of spanned access")]
        T e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15, e16;
        public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref e0, 17)[index];
        public readonly int Length => 17;
    }
}