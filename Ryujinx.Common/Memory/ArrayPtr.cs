using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Common.Memory
{
    public unsafe struct ArrayPtr<T> : IArray<T> where T : unmanaged
    {
        public static ArrayPtr<T> Null => new ArrayPtr<T>() { _ptr = 0 };

        private ulong _ptr;
        public bool IsNull => _ptr == 0;
        public int Length { get; }

        public ref T this[int index] => ref Unsafe.AsRef<T>((T*)_ptr + index);

        public ArrayPtr(ref T value, int length)
        {
            _ptr = (ulong)Unsafe.AsPointer(ref value);
            Length = length;
        }

        public ArrayPtr(T* ptr, int length)
        {
            _ptr = (ulong)ptr;
            Length = length;
        }

        public ArrayPtr(IntPtr ptr, int length)
        {
            _ptr = (ulong)ptr;
            Length = length;
        }

        public ArrayPtr<T> Slice(int start) => new ArrayPtr<T>(ref this[start], Length - start);
        public Span<T> ToSpan() => Length == 0 ? Span<T>.Empty : MemoryMarshal.CreateSpan(ref this[0], Length);
        public T* ToPointer() => (T*)_ptr;
    }
}
