using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Hid
{
    public struct Array2<T> where T : unmanaged
    {
        T e0, e1;
        public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref e0, 2)[index];
        public int Length { get { return 2; } }
    }

    public struct Array3<T> where T : unmanaged
    {
        T e0, e1, e2;
        public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref e0, 3)[index];
        public int Length { get { return 3; } }
    }

    public struct Array6<T> where T : unmanaged
    {
        T e0, e1, e2, e3, e4, e5;
        public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref e0, 6)[index];
        public int Length { get { return 6; } }
    }

    public struct Array7<T> where T : unmanaged
    {
        T e0, e1, e2, e3, e4, e5, e6;
        public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref e0, 7)[index];
        public int Length { get { return 7; } }
    }

    public struct Array10<T> where T : unmanaged
    {
        T e0, e1, e2, e3, e4, e5, e6, e7, e8, e9;
        public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref e0, 10)[index];
        public int Length { get { return 10; } }
    }

    public struct Array16<T> where T : unmanaged
    {
        T e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15;
        public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref e0, 16)[index];
        public int Length { get { return 16; } }
    }

    public struct Array17<T> where T : unmanaged
    {
        T e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15, e16;
        public ref T this[int index] => ref MemoryMarshal.CreateSpan(ref e0, 17)[index];
        public int Length { get { return 17; } }
    }


}