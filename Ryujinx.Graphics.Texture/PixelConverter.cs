using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Ryujinx.Graphics.Texture
{
    public static class PixelConverter
    {
        public unsafe static byte[] ConvertR4G4ToR4G4B4A4(ReadOnlySpan<byte> data)
        {
            byte[] output = new byte[data.Length * 2];
            int start = 0;

            if (Sse41.IsSupported)
            {
                int sizeTrunc = data.Length & ~7;
                start = sizeTrunc;

                fixed (byte* inputPtr = data, outputPtr = output)
                {
                    for (ulong offset = 0; offset < (ulong)sizeTrunc; offset += 8)
                    {
                        Sse2.Store(outputPtr + offset * 2, Sse41.ConvertToVector128Int16(inputPtr + offset).AsByte());
                    }
                }
            }

            Span<ushort> outputSpan = MemoryMarshal.Cast<byte, ushort>(output);

            for (int i = start; i < data.Length; i++)
            {
                outputSpan[i] = (ushort)data[i];
            }

            return output;
        }

        public unsafe static byte[] ConvertR5G6B5ToRGBA8(ReadOnlySpan<byte> data)
        {
            byte[] output = new byte[data.Length * 2];
            int start = 0;

            ReadOnlySpan<ushort> inputSpan = MemoryMarshal.Cast<byte, ushort>(data);
            Span<uint> outputSpan = MemoryMarshal.Cast<byte, uint>(output);

            float factor5Bit = 255.499f/31f;
            float factor6Bit = 255.499f/63f;

            for (int i = start; i < inputSpan.Length; i++)
            {
                ushort packed = inputSpan[i];
                uint r = (uint)((packed & 31) * factor5Bit);
                uint g = (uint)(((packed >> 5) & 63) * factor6Bit);
                uint b = (uint)(((packed >> 11) & 31) * factor5Bit);

                outputSpan[i] = r | (g << 8) | (b << 16) | 0xFF000000;
            }

            return output;
        }

        public unsafe static byte[] ConvertR5G5B5ToRGBA8(ReadOnlySpan<byte> data, bool forceAlpha)
        {
            byte[] output = new byte[data.Length * 2];
            int start = 0;

            ReadOnlySpan<ushort> inputSpan = MemoryMarshal.Cast<byte, ushort>(data);
            Span<uint> outputSpan = MemoryMarshal.Cast<byte, uint>(output);

            float factor5Bit = 255.499f / 31f;

            for (int i = start; i < inputSpan.Length; i++)
            {
                ushort packed = inputSpan[i];

                uint a = (uint)(forceAlpha ? 1 : (packed >> 15)) * 255u;
                uint r = (uint)((packed & 31) * factor5Bit) & a;
                uint g = (uint)(((packed >> 5) & 31) * factor5Bit) & a;
                uint b = (uint)(((packed >> 10) & 31) * factor5Bit) & a;

                outputSpan[i] = r | (g << 8) | (b << 16) | (a << 24);
            }

            return output;
        }

        public unsafe static byte[] ConvertRGBA4ToRGBA8(ReadOnlySpan<byte> data)
        {
            byte[] output = new byte[data.Length * 2];
            int start = 0;

            ReadOnlySpan<ushort> inputSpan = MemoryMarshal.Cast<byte, ushort>(data);
            Span<uint> outputSpan = MemoryMarshal.Cast<byte, uint>(output);

            for (int i = start; i < inputSpan.Length; i++)
            {
                uint packed = inputSpan[i];

                uint outputPacked =  packed         & 0x0000000f;
                     outputPacked |= (packed << 4)  & 0x00000f00;
                     outputPacked |= (packed << 8)  & 0x000f0000;
                     outputPacked |= (packed << 12) & 0x0f000000;

                outputSpan[i] = outputPacked * 0x11;
            }

            return output;
        }
    }
}
