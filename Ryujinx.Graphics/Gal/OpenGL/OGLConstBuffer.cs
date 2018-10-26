using OpenTK.Graphics.OpenGL;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    internal class OGLConstBuffer : IGalConstBuffer
    {
        private OGLCachedResource<OGLStreamBuffer> _cache;

        public OGLConstBuffer()
        {
            _cache = new OGLCachedResource<OGLStreamBuffer>(DeleteBuffer);
        }

        public void LockCache()
        {
            _cache.Lock();
        }

        public void UnlockCache()
        {
            _cache.Unlock();
        }

        public void Create(long key, long size)
        {
            OGLStreamBuffer buffer = new OGLStreamBuffer(BufferTarget.UniformBuffer, size);

            _cache.AddOrUpdate(key, buffer, size);
        }

        public bool IsCached(long key, long size)
        {
            return _cache.TryGetSize(key, out long cachedSize) && cachedSize == size;
        }

        public void SetData(long key, long size, IntPtr hostAddress)
        {
            if (_cache.TryGetValue(key, out OGLStreamBuffer buffer))
            {
                buffer.SetData(size, hostAddress);
            }
        }

        public bool TryGetUbo(long key, out int uboHandle)
        {
            if (_cache.TryGetValue(key, out OGLStreamBuffer buffer))
            {
                uboHandle = buffer.Handle;

                return true;
            }

            uboHandle = 0;

            return false;
        }

        private static void DeleteBuffer(OGLStreamBuffer buffer)
        {
            buffer.Dispose();
        }
    }
}