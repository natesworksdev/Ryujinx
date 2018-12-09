namespace Ryujinx.Graphics.Gal.OpenGL
{
    static class OGLResourceLimits
    {
        private const int KB = 1024;
        private const int MB = 1024 * KB;

        public const int ConstBufferLimit = 64 * MB;

        public const int VertexArrayLimit  = 16384;
        public const int VertexBufferLimit = 128 * MB;
        public const int IndexBufferLimit  = 64  * MB;

        public const int TextureLimit = 768 * MB;

        public const int PixelBufferLimit = 64 * MB;
    }
}