using OpenTK.Graphics.OpenGL;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class OGLConstBuffer : IGalConstBuffer
    {
        private const long MaxConstBufferCacheSize = 64 * 1024 * 1024;

        private OGLResourceCache<long, OGLStreamBuffer> Cache;

        public OGLConstBuffer()
        {
            Cache = new OGLResourceCache<long, OGLStreamBuffer>(DeleteBuffer, MaxConstBufferCacheSize);
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

        public bool IsCached(long key, long size)
        {
            return Cache.TryGetSize(key, out long cbSize) && cbSize >= size;
        }

        public void Create(long key, IntPtr hostAddress, long size)
        {
            GetBuffer(key, size).SetData(size, hostAddress);
        }

        public void Create(long key, byte[] data)
        {
            GetBuffer(key, data.Length).SetData(data);
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

        private OGLStreamBuffer GetBuffer(long Key, long Size)
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