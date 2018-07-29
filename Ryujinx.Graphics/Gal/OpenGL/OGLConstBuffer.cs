using OpenTK.Graphics.OpenGL;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class OGLConstBuffer : IGalConstBuffer
    {
        public const int ConstBuffersPerStage = 18;

        private OGLCachedResource<OGLStreamBuffer> Cache;

        private long[][] Keys;

        public OGLConstBuffer()
        {
            Cache = new OGLCachedResource<OGLStreamBuffer>(DeleteBuffer);

            Keys = new long[5][];

            for (int i = 0; i < Keys.Length; i++)
            {
                Keys[i] = new long[ConstBuffersPerStage];
            }
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
            OGLStreamBuffer Buffer = new OGLStreamBuffer(BufferTarget.UniformBuffer, Size);

            Cache.AddOrUpdate(Key, Buffer, Size);
        }

        public bool IsCached(long Key, long Size)
        {
            return Cache.TryGetSize(Key, out long CachedSize) && CachedSize == Size;
        }

        public void SetData(long Key, long Size, IntPtr HostAddress)
        {
            if (!Cache.TryGetValue(Key, out OGLStreamBuffer Buffer))
            {
                throw new InvalidOperationException();
            }

            Buffer.SetData(Size, HostAddress);
        }

        public void Bind(GalShaderType Stage, int Index, long Key)
        {
            Keys[(int)Stage][Index] = Key;
        }

        public void PipelineBind(GalShaderType Stage, int Index, int BindingIndex)
        {
            long Key = Keys[(int)Stage][Index];

            if (Key != 0 && Cache.TryGetValue(Key, out OGLStreamBuffer Buffer))
            {
                GL.BindBufferBase(BufferRangeTarget.UniformBuffer, BindingIndex, Buffer.Handle);
            }
        }

        private static void DeleteBuffer(OGLStreamBuffer Buffer)
        {
            Buffer.Dispose();
        }
    }
}