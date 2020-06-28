using System.Runtime.CompilerServices;

namespace Ryujinx.Common.Memory
{
    public unsafe struct Ptr<T> where T : unmanaged
    {
        public static Ptr<T> Null => new Ptr<T>() { _ptr = 0 };
        private ulong _ptr;
        public bool IsNull => _ptr == 0;
        public ref T Value => ref Unsafe.AsRef<T>((void*)_ptr);
        public Ptr(ref T value)
        {
            _ptr = (ulong)Unsafe.AsPointer(ref value);
        }
    }
}
