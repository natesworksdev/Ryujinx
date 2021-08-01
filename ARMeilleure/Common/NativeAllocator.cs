using System;
using System.Runtime.InteropServices;

namespace ARMeilleure.Common
{
    unsafe class NativeAllocator : IAllocator
    {
        public static NativeAllocator Instance { get; } = new();

        public void* Allocate(int size)
        {
            void* result = (void*)Marshal.AllocHGlobal(size);

            if (result == null)
            {
                throw new OutOfMemoryException();
            }

            return result;
        }

        public void Free(void* block)
        {
            Marshal.FreeHGlobal((IntPtr)block);
        }

        public void Dispose() { }
    }
}
