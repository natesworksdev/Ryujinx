namespace Ryujinx.Graphics.Gpu.Shader.DiskCache
{
    /// <summary>
    /// Algorithm used to compress the cache.
    /// </summary>
    enum CompressionAlgorithm : byte
    {
        None,
        Deflate
    }
}