using OpenTK.Graphics.OpenGL;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class OGLConstBuffer : IGalConstBuffer
    {
        private OGLResourceCache<int, OGLStreamBuffer> Cache;

        public OGLConstBuffer()
        {
            Cache = new OGLResourceCache<int, OGLStreamBuffer>(DeleteBuffer, OGLResourceLimits.ConstBufferLimit);
        }

        private static void DeleteBuffer(OGLStreamBuffer Buffer)
        {
            Buffer.Dispose();
        }

        public void LockCache()
        {
            Cache.Lock();
        }

        public void UnlockCache()
        {
            Cache.Unlock();
        }

        public bool IsCached(long key, int size)
        {
            return Cache.TryGetSize(key, out int cbSize) && cbSize >= size;
        }

        public void Create(long key, IntPtr hostAddress, int size)
        {
            GetBuffer(key, size).SetData(hostAddress, size);
        }

        public void Create(long key, byte[] buffer)
        {
            GetBuffer(key, buffer.Length).SetData(buffer);
        }

        public bool TryGetUbo(long key, out int uboHandle)
        {
            if (Cache.TryGetValue(key, out OGLStreamBuffer buffer))
            {
                uboHandle = buffer.Handle;

                return true;
            }

            uboHandle = 0;

            return false;
        }

        private OGLStreamBuffer GetBuffer(long Key, int Size)
        {
            if (!Cache.TryReuseValue(Key, Size, out OGLStreamBuffer Buffer))
            {
                Buffer = new OGLStreamBuffer(BufferTarget.UniformBuffer, Size);

                Cache.AddOrUpdate(Key, Size, Buffer, Size);
            }

            return Buffer;
        }
    }
}