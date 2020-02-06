using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Gpu.Shader
{
    struct Fnv1a
    {
        private const int Prime = 0x1000193;
        private const int OffsetBasis = unchecked((int)0x811c9dc5);

        public int Hash { get; private set; }

        public void Initialize()
        {
            Hash = OffsetBasis;
        }

        public void Add(ReadOnlySpan<byte> data)
        {
            ReadOnlySpan<int> dataInt = MemoryMarshal.Cast<byte, int>(data);

            int offset;

            for (offset = 0; offset < dataInt.Length; offset++)
            {
                Hash = (Hash ^ data[offset]) * Prime;
            }

            for (offset *= 4; offset < data.Length; offset++)
            {
                Hash = (Hash ^ data[offset]) * Prime;
            }
        }
    }
}
