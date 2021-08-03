using System;

namespace ARMeilleure.Common
{
    unsafe abstract class Allocator : IDisposable
    {
        public T* Allocate<T>(int count = 1) where T : unmanaged
        {
            return (T*)Allocate(count * sizeof(T));
        }

        public abstract void* Allocate(int size);

        public abstract void Free(void* block);

        protected virtual void Dispose(bool disposing) { }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
