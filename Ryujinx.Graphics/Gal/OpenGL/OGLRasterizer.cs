using OpenTK.Graphics.OpenGL;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class OGLRasterizer : IGalRasterizer
    {
        public bool DepthWriteEnabled { set; private get; }

        private int[] VertexBuffers;

        private OGLCachedResource<BufferParams, OGLStreamBuffer> VboCache;
        private OGLCachedResource<BufferParams, OGLStreamBuffer> IboCache;

        private struct IbInfo
        {
            public int Count;
            public int ElemSizeLog2;

            public DrawElementsType Type;
        }

        private IbInfo IndexBuffer;

        public OGLRasterizer()
        {
            VertexBuffers = new int[32];

            VboCache = new OGLCachedResource<BufferParams, OGLStreamBuffer>(CreateBuffer, DeleteBuffer);
            IboCache = new OGLCachedResource<BufferParams, OGLStreamBuffer>(CreateBuffer, DeleteBuffer);

            IndexBuffer = new IbInfo();

            DepthWriteEnabled = true;
        }

        public void LockCaches()
        {
            VboCache.Lock();
            IboCache.Lock();
        }

        public void UnlockCaches()
        {
            VboCache.Unlock();
            IboCache.Unlock();
        }

        public void ClearBuffers(
            GalClearBufferFlags Flags,
            int Attachment,
            float Red, float Green, float Blue, float Alpha,
            float Depth,
            int Stencil)
        {
            //OpenGL needs glDepthMask to be enabled to clear it
            if (!DepthWriteEnabled)
            {
                GL.DepthMask(true);
            }

            GL.ColorMask(
                Flags.HasFlag(GalClearBufferFlags.ColorRed),
                Flags.HasFlag(GalClearBufferFlags.ColorGreen),
                Flags.HasFlag(GalClearBufferFlags.ColorBlue),
                Flags.HasFlag(GalClearBufferFlags.ColorAlpha));

            GL.ClearBuffer(ClearBuffer.Color, Attachment, new float[] { Red, Green, Blue, Alpha });

            if (Flags.HasFlag(GalClearBufferFlags.Depth))
            {
                GL.ClearBuffer(ClearBuffer.Depth, 0, ref Depth);
            }

            if (Flags.HasFlag(GalClearBufferFlags.Stencil))
            {
                GL.ClearBuffer(ClearBuffer.Stencil, 0, ref Stencil);
            }

            GL.ColorMask(true, true, true, true);

            if (!DepthWriteEnabled)
            {
                GL.DepthMask(false);
            }
        }

        public bool IsVboCached(long Key, long DataSize)
        {
            return VboCache.TryGetSize(Key, out long Size) && Size == DataSize;
        }

        public bool IsIboCached(long Key, long DataSize)
        {
            return IboCache.TryGetSize(Key, out long Size) && Size == DataSize;
        }

        public void CreateVbo(long Key, int DataSize, IntPtr HostAddress)
        {
            BufferParams Params = new BufferParams(BufferTarget.ArrayBuffer, DataSize);

            OGLStreamBuffer CachedBuffer = VboCache.CreateOrRecycle(Key, Params, (uint)DataSize);

            CachedBuffer.SetData(DataSize, HostAddress);
        }

        public void CreateIbo(long Key, int DataSize, IntPtr HostAddress)
        {
            BufferParams Params = new BufferParams(BufferTarget.ElementArrayBuffer, DataSize);

            OGLStreamBuffer CachedBuffer = IboCache.CreateOrRecycle(Key, Params, (uint)DataSize);

            CachedBuffer.SetData(DataSize, HostAddress);
        }

        public void SetIndexArray(int Size, GalIndexFormat Format)
        {
            IndexBuffer.Type = OGLEnumConverter.GetDrawElementsType(Format);

            IndexBuffer.Count = Size >> (int)Format;

            IndexBuffer.ElemSizeLog2 = (int)Format;
        }

        public void DrawArrays(int First, int Count, GalPrimitiveType PrimType)
        {
            if (Count == 0)
            {
                return;
            }

            GL.DrawArrays(OGLEnumConverter.GetPrimitiveType(PrimType), First, Count);
        }

        public void DrawElements(long IboKey, int First, int VertexBase, GalPrimitiveType PrimType)
        {
            if (!IboCache.TryGetValue(IboKey, out OGLStreamBuffer CachedBuffer))
            {
                return;
            }

            PrimitiveType Mode = OGLEnumConverter.GetPrimitiveType(PrimType);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, CachedBuffer.Handle);

            First <<= IndexBuffer.ElemSizeLog2;

            if (VertexBase != 0)
            {
                IntPtr Indices = new IntPtr(First);

                GL.DrawElementsBaseVertex(Mode, IndexBuffer.Count, IndexBuffer.Type, Indices, VertexBase);
            }
            else
            {
                GL.DrawElements(Mode, IndexBuffer.Count, IndexBuffer.Type, First);
            }
        }

        public bool TryGetVbo(long VboKey, out int VboHandle)
        {
            if (VboCache.TryGetValue(VboKey, out OGLStreamBuffer CachedBuffer))
            {
                VboHandle = CachedBuffer.Handle;

                return true;
            }

            VboHandle = 0;

            return false;
        }

        private static OGLStreamBuffer CreateBuffer(BufferParams Params)
        {
            return new OGLStreamBuffer(Params.Target, (int)Params.Size);
        }

        private static void DeleteBuffer(OGLStreamBuffer CachedBuffer)
        {
            CachedBuffer.Dispose();
        }
    }
}