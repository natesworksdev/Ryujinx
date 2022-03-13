namespace Ryujinx.Graphics.Gpu.Shader
{
    static class DiskCacheCommon
    {
        public static CompressionAlgorithm GetCompressionAlgorithm()
        {
            return CompressionAlgorithm.Deflate;
        }
    }
}