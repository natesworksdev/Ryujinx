using System;

namespace Ryujinx.Graphics
{
    internal static class QuadHelper
    {
        public static int ConvertIbSizeQuadsToTris(int size)
        {
            return size <= 0 ? 0 : size / 4 * 6;
        }

        public static int ConvertIbSizeQuadStripToTris(int size)
        {
            return size <= 1 ? 0 : (size - 2) / 2 * 6;
        }

        public static byte[] ConvertIbQuadsToTris(byte[] data, int entrySize, int count)
        {
            int primitivesCount = count / 4;

            int quadPrimSize = 4 * entrySize;
            int trisPrimSize = 6 * entrySize;

            byte[] output = new byte[primitivesCount * 6 * entrySize];

            for (int prim = 0; prim < primitivesCount; prim++)
            {
                void AssignIndex(int src, int dst, int copyCount = 1)
                {
                    src = prim * quadPrimSize + src * entrySize;
                    dst = prim * trisPrimSize + dst * entrySize;

                    Buffer.BlockCopy(data, src, output, dst, copyCount * entrySize);
                }

                //0 1 2 -> 0 1 2.
                AssignIndex(0, 0, 3);

                //2 3 -> 3 4.
                AssignIndex(2, 3, 2);

                //0 -> 5.
                AssignIndex(0, 5);
            }

            return output;
        }

        public static byte[] ConvertIbQuadStripToTris(byte[] data, int entrySize, int count)
        {
            int primitivesCount = (count - 2) / 2;

            int quadPrimSize = 2 * entrySize;
            int trisPrimSize = 6 * entrySize;

            byte[] output = new byte[primitivesCount * 6 * entrySize];

            for (int prim = 0; prim < primitivesCount; prim++)
            {
                void AssignIndex(int src, int dst, int copyCount = 1)
                {
                    src = prim * quadPrimSize + src * entrySize + 2 * entrySize;
                    dst = prim * trisPrimSize + dst * entrySize;

                    Buffer.BlockCopy(data, src, output, dst, copyCount * entrySize);
                }

                //-2 -1 0 -> 0 1 2.
                AssignIndex(-2, 0, 3);

                //0 1 -> 3 4.
                AssignIndex(0, 3, 2);

                //-2 -> 5.
                AssignIndex(-2, 5);
            }

            return output;
        }
    }
}