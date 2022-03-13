using System.IO;

namespace Ryujinx.Graphics.Gpu.Shader
{
    static class DiskCacheCommon
    {
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

        public static CompressionAlgorithm GetCompressionAlgorithm()
        {
            return CompressionAlgorithm.Deflate;
        }
    }
}