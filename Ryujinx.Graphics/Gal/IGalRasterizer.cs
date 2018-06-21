namespace Ryujinx.Graphics.Gal
{
    public interface IGalRasterizer
    {
        void ClearBuffers(int RtIndex, GalClearBufferFlags Flags);

        bool IsVboCached(long Tag, long DataSize);

        bool IsIboCached(long Tag, long DataSize);

        void CreateVbo(long Tag, byte[] Buffer);

        void CreateIbo(long Tag, byte[] Buffer);

        void SetVertexArray(int VbIndex, int Stride, long VboTag, GalVertexAttrib[] Attribs);

        void SetIndexArray(long Tag, int Size, GalIndexFormat Format);

        void DrawArrays(int First, int PrimCount, GalPrimitiveType PrimType);

        void DrawElements(long IboTag, int First, GalPrimitiveType PrimType);
    }
}