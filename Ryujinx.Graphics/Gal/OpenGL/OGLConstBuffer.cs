using OpenTK.Graphics.OpenGL;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class OGLConstBuffer : IGalConstBuffer
    {
        private OGLCachedResource<BufferParams, OGLStreamBuffer> Cache;

        public OGLConstBuffer()
        {
            Cache = new OGLCachedResource<BufferParams, OGLStreamBuffer>(CreateBuffer, DeleteBuffer);
        }

        public void LockCache()
        {
            Cache.Lock();
        }

        public void UnlockCache()
        {
            Cache.Unlock();
        }

        public void Create(long Key, long Size)
        {
            BufferParams Params = new BufferParams(BufferTarget.UniformBuffer, Size);

            Cache.CreateOrRecycle(Key, Params, Size);
        }

        public bool IsCached(long Key, long Size)
        {
            return Cache.TryGetSize(Key, out long CachedSize) && CachedSize == Size;
        }

        public void SetData(long Key, long Size, IntPtr HostAddress)
        {
            if (Cache.TryGetValue(Key, out OGLStreamBuffer Buffer))
            {
                Buffer.SetData(Size, HostAddress);
            }
        }

        public bool TryGetUbo(long Key, out int UboHandle)
        {
            if (Cache.TryGetValue(Key, out OGLStreamBuffer Buffer))
            {
                UboHandle = Buffer.Handle;

                return true;
            }

            UboHandle = 0;

            return false;
        }

        private static OGLStreamBuffer CreateBuffer(BufferParams Params)
        {
            return new OGLStreamBuffer(Params.Target, Params.Size);
        }

        private static void DeleteBuffer(OGLStreamBuffer Buffer)
        {
            Buffer.Dispose();
        }
    }
}