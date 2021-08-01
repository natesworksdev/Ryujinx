using System;

namespace ARMeilleure.Common
{
    unsafe interface IAllocator : IDisposable
    {
        T* Allocate<T>(int count) where T : unmanaged
        {
            return (T*)Allocate(count * sizeof(T));
        }

        void* Allocate(int size);

        void Free(void* block);
    }
}
