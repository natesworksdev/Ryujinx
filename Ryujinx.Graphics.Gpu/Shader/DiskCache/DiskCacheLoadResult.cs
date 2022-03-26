namespace Ryujinx.Graphics.Gpu.Shader.DiskCache
{
    enum DiskCacheLoadResult
    {
        Success,
        NoAccess,
        FileCorruptedGeneric,
        FileCorruptedCompressionError,
        FileCorruptedInvalidMagic,
        FileCorruptedInvalidLength,
        IncompatibleVersion
    }

    static class DiskCacheLoadResultExtensions
    {
        public static string GetMessage(this DiskCacheLoadResult result)
        {
            return result switch
            {
                DiskCacheLoadResult.Success => "No error.",
                DiskCacheLoadResult.NoAccess => "Could not access the cache file.",
                DiskCacheLoadResult.FileCorruptedGeneric => "The cache file is corrupted.",
                DiskCacheLoadResult.FileCorruptedCompressionError => "Decompression failed, the cache file is corrupted.",
                DiskCacheLoadResult.FileCorruptedInvalidMagic => "Magic check failed, the cache file is corrupted.",
                DiskCacheLoadResult.FileCorruptedInvalidLength => "Length check failed, the cache file is corrupted.",
                DiskCacheLoadResult.IncompatibleVersion => "The version of the disk cache is not compatible with this version of the emulator.",
                _ => "Unknown error."
            };
        }
    }
}