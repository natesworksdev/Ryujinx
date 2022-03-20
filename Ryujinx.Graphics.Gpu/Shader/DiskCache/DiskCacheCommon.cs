using System.IO;

namespace Ryujinx.Graphics.Gpu.Shader.DiskCache
{
    /// <summary>
    /// Common disk cache utility methods.
    /// </summary>
    static class DiskCacheCommon
    {
        /// <summary>
        /// Opens a file for read of write.
        /// </summary>
        /// <param name="basePath">Base path of the file (should not include the file name)</param>
        /// <param name="fileName">Name of the file</param>
        /// <param name="writable">Indicates if the file will be read or written</param>
        /// <returns>File stream</returns>
        public static FileStream OpenFile(string basePath, string fileName, bool writable)
        {
            Directory.CreateDirectory(basePath);
            string fullPath = Path.Combine(basePath, fileName);

            FileMode mode;
            FileAccess access;

            if (writable)
            {
                mode = FileMode.OpenOrCreate;
                access = FileAccess.ReadWrite;
            }
            else
            {
                mode = FileMode.Open;
                access = FileAccess.Read;
            }

            return new FileStream(fullPath, mode, access, FileShare.Read);
        }

        /// <summary>
        /// Gets the compression algorithm that should be used when writing the disk cache.
        /// </summary>
        /// <returns>Compression algorithm</returns>
        public static CompressionAlgorithm GetCompressionAlgorithm()
        {
            return CompressionAlgorithm.Deflate;
        }
    }
}