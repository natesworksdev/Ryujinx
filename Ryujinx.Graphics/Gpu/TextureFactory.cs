using ChocolArm64.Memory;
using Ryujinx.Graphics.Gal;

namespace Ryujinx.Graphics.Gpu
{
    static class TextureFactory
    {
        public static GalTexture MakeTexture(NsGpu Gpu, AMemory Memory, long TicPosition)
        {
            int[] Tic = ReadWords(Memory, TicPosition, 8);

            GalTextureFormat Format = (GalTextureFormat)(Tic[0] & 0x7f);

            long TextureAddress = (uint)Tic[1];

            TextureAddress |= (long)((ushort)Tic[2]) << 32;

            TextureAddress = Gpu.GetCpuAddr(TextureAddress);

            TextureSwizzle Swizzle = (TextureSwizzle)((Tic[2] >> 21) & 7);

            int BlockHeightLog2 = (Tic[3] >> 3) & 7;

            int BlockHeight = 1 << BlockHeightLog2;

            int Width  = (Tic[4] & 0xffff) + 1;
            int Height = (Tic[5] & 0xffff) + 1;

            Texture Texture = new Texture(
                TextureAddress,
                Width,
                Height,
                BlockHeight,
                Swizzle,
                Format);

            byte[] Data = TextureReader.Read(Memory, Texture);

            return new GalTexture(Data, Width, Height, Format);
        }

        private static int[] ReadWords(AMemory Memory, long Position, int Count)
        {
            int[] Words = new int[Count];

            for (int Index = 0; Index < Count; Index++, Position += 4)
            {
                Words[Index] = Memory.ReadInt32(Position);
            }

            return Words;
        }
    }
}