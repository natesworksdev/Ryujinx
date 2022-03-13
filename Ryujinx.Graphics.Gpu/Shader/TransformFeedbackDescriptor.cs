using Ryujinx.Common.Memory;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Gpu.Shader
{
    struct TransformFeedbackDescriptor
    {
        public readonly int BufferIndex;
        public readonly int Stride;
        public readonly Array32<uint> VaryingLocations;

        public TransformFeedbackDescriptor(int bufferIndex, int stride, ref Array32<uint> varyingLocations)
        {
            BufferIndex = bufferIndex;
            Stride = stride;
            VaryingLocations = varyingLocations;
        }

        public ReadOnlySpan<byte> AsSpan()
        {
            return MemoryMarshal.Cast<uint, byte>(VaryingLocations.ToSpan());
        }
    }
}
