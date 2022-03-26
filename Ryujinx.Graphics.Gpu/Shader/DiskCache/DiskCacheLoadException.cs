using System;

namespace Ryujinx.Graphics.Gpu.Shader.DiskCache
{
    class DiskCacheLoadException : Exception
    {
        public DiskCacheLoadResult Result { get; }

        public DiskCacheLoadException()
        {
        }

        public DiskCacheLoadException(string message) : base(message)
        {
        }

        public DiskCacheLoadException(string message, Exception inner) : base(message, inner)
        {
        }

        public DiskCacheLoadException(DiskCacheLoadResult result) : base(result.GetMessage())
        {
            Result = result;
        }
    }
}