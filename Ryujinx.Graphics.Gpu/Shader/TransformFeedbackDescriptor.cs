using Ryujinx.Common.Memory;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Gpu.Shader
{
    struct TransformFeedbackDescriptor
    {
        public readonly int BufferIndex;
        public readonly int Stride;
        public readonly int VaryingCount;
        public readonly Array32<uint> VaryingLocations;

        public TransformFeedbackDescriptor(int bufferIndex, int stride, int varyingCount, Array32<uint> varyingLocations)
        {
            BufferIndex = bufferIndex;
            Stride = stride;
            VaryingCount = varyingCount;
            VaryingLocations = varyingLocations;
        }

        public ReadOnlySpan<byte> AsSpan()
        {
            byte[] temp = new byte[Math.Min(128, VaryingCount)];
            MemoryMarshal.Cast<uint, byte>(VaryingLocations.ToSpan()).Slice(0, temp.Length).CopyTo(temp);
            return temp;

            // This doesn't work, it just reads garbage values. Might be a .NET bug.
            // return MemoryMarshal.Cast<uint, byte>(VaryingLocations.ToSpan()).Slice(0, Math.Min(128, VaryingCount));
        }
    }
}
