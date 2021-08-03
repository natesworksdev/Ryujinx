using System;
using System.Runtime.InteropServices;

namespace ARMeilleure.Common
{
    unsafe class NativeAllocator : Allocator
    {
        public static NativeAllocator Instance { get; } = new();

        public override void* Allocate(int size)
        {
            void* result = (void*)Marshal.AllocHGlobal(size);

            if (result == null)
            {
                throw new OutOfMemoryException();
            }

            return result;
        }

        public override void Free(void* block)
        {
            Marshal.FreeHGlobal((IntPtr)block);
        }
    }
}
