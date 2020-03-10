using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// FNV1a hash calculation.
    /// This is not a strict implementation of the FNV1a algorithm,
    /// it was optimized for speed.
    /// </summary>
    struct Fnv1a
    {
        private const int Prime = 0x1000193;
        private const int OffsetBasis = unchecked((int)0x811c9dc5);

        /// <summary>
        /// Current hash value.
        /// </summary>
        public int Hash { get; private set; }

        /// <summary>
        /// Initializes the hash value.
        /// </summary>
        public void Initialize()
        {
            Hash = OffsetBasis;
        }

        /// <summary>
        /// Hashes data and updates the current hash value.
        /// </summary>
        /// <param name="data">Data to be hashed</param>
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
