using Ryujinx.Common.Memory;
using System;
using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Ryujinx.Graphics.OpenGL.Image
{
    static class FormatConverter
    {
        public unsafe static IMemoryOwner<byte> ConvertS8D24ToD24S8(ReadOnlySpan<byte> data)
        {
            IMemoryOwner<byte> outputMemory = ByteMemoryPool.Rent(data.Length);

            Span<byte> output = outputMemory.Memory.Span;

            int start = 0;

            if (Avx2.IsSupported)
            {
                var mask = Vector256.Create(
                    3, 0, 1, 2,
                    7, 4, 5, 6,
                    11, 8, 9, 10,
                    15, 12, 13, 14,
                    19, 16, 17, 18,
                    23, 20, 21, 22,
                    27, 24, 25, 26,
                    31, 28, 29, (byte)30);

                int sizeAligned = data.Length & ~31;

                fixed (byte* pInput = data, pOutput = output)
                {
                    for (uint i = 0; i < sizeAligned; i += 32)
                    {
                        var dataVec = Avx.LoadVector256(pInput + i);

                        dataVec = Avx2.Shuffle(dataVec, mask);

                        Avx.Store(pOutput + i, dataVec);
                    }
                }

                start = sizeAligned;
            }
            else if (Ssse3.IsSupported)
            {
                var mask = Vector128.Create(
                    3, 0, 1, 2,
                    7, 4, 5, 6,
                    11, 8, 9, 10,
                    15, 12, 13, (byte)14);

                int sizeAligned = data.Length & ~15;

                fixed (byte* pInput = data, pOutput = output)
                {
                    for (uint i = 0; i < sizeAligned; i += 16)
                    {
                        var dataVec = Sse2.LoadVector128(pInput + i);

                        dataVec = Ssse3.Shuffle(dataVec, mask);

                        Sse2.Store(pOutput + i, dataVec);
                    }
                }

                start = sizeAligned;
            }

            var outSpan = MemoryMarshal.Cast<byte, uint>(output);
            var dataSpan = MemoryMarshal.Cast<byte, uint>(data);
            for (int i = start / sizeof(uint); i < dataSpan.Length; i++)
            {
                outSpan[i] = BitOperations.RotateLeft(dataSpan[i], 8);
            }

            return outputMemory;
        }

        public unsafe static byte[] ConvertD24S8ToS8D24(ReadOnlySpan<byte> data)
        {
            byte[] output = new byte[data.Length];

            int start = 0;

            if (Avx2.IsSupported)
            {
                var mask = Vector256.Create(
                    1, 2, 3, 0,
                    5, 6, 7, 4,
                    9, 10, 11, 8,
                    13, 14, 15, 12,
                    17, 18, 19, 16,
                    21, 22, 23, 20,
                    25, 26, 27, 24,
                    29, 30, 31, (byte)28);

                int sizeAligned = data.Length & ~31;

                fixed (byte* pInput = data, pOutput = output)
                {
                    for (uint i = 0; i < sizeAligned; i += 32)
                    {
                        var dataVec = Avx.LoadVector256(pInput + i);

                        dataVec = Avx2.Shuffle(dataVec, mask);

                        Avx.Store(pOutput + i, dataVec);
                    }
                }

                start = sizeAligned;
            }
            else if (Ssse3.IsSupported)
            {
                var mask = Vector128.Create(
                    1, 2, 3, 0,
                    5, 6, 7, 4,
                    9, 10, 11, 8,
                    13, 14, 15, (byte)12);

                int sizeAligned = data.Length & ~15;

                fixed (byte* pInput = data, pOutput = output)
                {
                    for (uint i = 0; i < sizeAligned; i += 16)
                    {
                        var dataVec = Sse2.LoadVector128(pInput + i);

                        dataVec = Ssse3.Shuffle(dataVec, mask);

                        Sse2.Store(pOutput + i, dataVec);
                    }
                }

                start = sizeAligned;
            }

            var outSpan = MemoryMarshal.Cast<byte, uint>(output);
            var dataSpan = MemoryMarshal.Cast<byte, uint>(data);
            for (int i = start / sizeof(uint); i < dataSpan.Length; i++)
            {
                outSpan[i] = BitOperations.RotateRight(dataSpan[i], 8);
            }

            return output;
        }
    }
}
