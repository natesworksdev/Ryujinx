using Ryujinx.Graphics.Gal;
using System;
using System.Buffers.Binary;

namespace Ryujinx.Graphics.Graphics3d
{
    static class IbHelper
    {
        public static int ConvertIbSizeQuadsToTris(int Size)
        {
            return Size <= 0 ? 0 : (Size / 4) * 6;
        }

        public static int ConvertIbSizeQuadStripToTris(int Size)
        {
            return Size <= 1 ? 0 : ((Size - 2) / 2) * 6;
        }

        public static byte[] ConvertIbQuadsToTris(byte[] Data, int EntrySize, int Count)
        {
            int PrimitivesCount = Count / 4;

            int QuadPrimSize = 4 * EntrySize;
            int TrisPrimSize = 6 * EntrySize;

            byte[] Output = new byte[PrimitivesCount * 6 * EntrySize];

            for (int Prim = 0; Prim < PrimitivesCount; Prim++)
            {
                void AssignIndex(int Src, int Dst, int CopyCount = 1)
                {
                    Src = Prim * QuadPrimSize + Src * EntrySize;
                    Dst = Prim * TrisPrimSize + Dst * EntrySize;

                    Buffer.BlockCopy(Data, Src, Output, Dst, CopyCount * EntrySize);
                }

                //0 1 2 -> 0 1 2.
                AssignIndex(0, 0, 3);

                //2 3 -> 3 4.
                AssignIndex(2, 3, 2);

                //0 -> 5.
                AssignIndex(0, 5);
            }

            return Output;
        }

        public static byte[] ConvertIbQuadStripToTris(byte[] Data, int EntrySize, int Count)
        {
            int PrimitivesCount = (Count - 2) / 2;

            int QuadPrimSize = 2 * EntrySize;
            int TrisPrimSize = 6 * EntrySize;

            byte[] Output = new byte[PrimitivesCount * 6 * EntrySize];

            for (int Prim = 0; Prim < PrimitivesCount; Prim++)
            {
                void AssignIndex(int Src, int Dst, int CopyCount = 1)
                {
                    Src = Prim * QuadPrimSize + Src * EntrySize + 2 * EntrySize;
                    Dst = Prim * TrisPrimSize + Dst * EntrySize;

                    Buffer.BlockCopy(Data, Src, Output, Dst, CopyCount * EntrySize);
                }

                //-2 -1 0 -> 0 1 2.
                AssignIndex(-2, 0, 3);

                //0 1 -> 3 4.
                AssignIndex(0, 3, 2);

                //-2 -> 5.
                AssignIndex(-2, 5);
            }

            return Output;
        }

        public static int GetVertexCountFromIb16(byte[] data)
        {
            if (data.Length == 0)
            {
                return 0;
            }

            ushort max = 0;

            for (int index = 0; index < data.Length; index += 2)
            {
                ushort value = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(index, 2));

                if (max < value)
                {
                    max = value;
                }
            }

            return max + 1;
        }

        public static long GetVertexCountFromIb32(byte[] data)
        {
            if (data.Length == 0)
            {
                return 0;
            }

            uint max = 0;

            for (int index = 0; index < data.Length; index += 4)
            {
                uint value = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(index, 4));

                if (max < value)
                {
                    max = value;
                }
            }

            return max + 1;
        }

        public static long GetIbMaxVertexCount(GalIndexFormat format)
        {
            switch (format)
            {
                case GalIndexFormat.Byte:  return 1L << 8;
                case GalIndexFormat.Int16: return 1L << 16;
                case GalIndexFormat.Int32: return 1L << 32;
            }

            throw new ArgumentException(nameof(format));
        }
    }
}