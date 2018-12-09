using System;

namespace Ryujinx.Graphics.Gal
{
    public interface IGalRasterizer
    {
        void LockCaches();
        void UnlockCaches();

        void ClearBuffers(
            GalClearBufferFlags Flags,
            int Attachment,
            float Red,
            float Green,
            float Blue,
            float Alpha,
            float Depth,
            int Stencil);

        bool TryBindVao(ReadOnlySpan<int> rawAttributes, GalVertexAttribArray[] arrays);

        void CreateVao(
            ReadOnlySpan<int>      rawAttributes,
            GalVertexAttrib[]      attributes,
            GalVertexAttribArray[] arrays);

        bool IsVboCached(long key, long size);
        bool IsIboCached(long key, long size, out long vertexCount);

        void CreateVbo(long Key, int DataSize, IntPtr HostAddress);
        void CreateVbo(long Key, byte[] Data);

        void CreateIbo(long key, IntPtr hostAddress, int size, long vertexCount);
        void CreateIbo(long key, byte[] buffer, long vertexCount);

        void SetIndexArray(int Size, GalIndexFormat Format);

        void DrawArrays(int First, int Count, GalPrimitiveType PrimType);

        void DrawElements(long IboKey, int First, int VertexBase, GalPrimitiveType PrimType);
    }
}