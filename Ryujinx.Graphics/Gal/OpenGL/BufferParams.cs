using OpenTK.Graphics.OpenGL;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class BufferParams : ICompatible<BufferParams>
    {
        public BufferTarget Target { get; private set; }

        public long Size { get; private set; }

        public BufferParams(BufferTarget Target, long Size)
        {
            this.Target = Target;
            this.Size = Size;
        }

        public bool IsCompatible(BufferParams Other)
        {
            //Target is not needed for compatibility

            return Size == Other.Size;
        }
    }
}