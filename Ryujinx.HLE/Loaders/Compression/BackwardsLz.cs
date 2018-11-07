using System;
using System.IO;

namespace Ryujinx.HLE.Loaders.Compression
{
    static class BackwardsLz
    {
        private class BackwardsReader
        {
            private Stream BaseStream;

            public BackwardsReader(Stream BaseStream)
            {
                this.BaseStream = BaseStream;
            }

            public byte ReadByte()
            {
                BaseStream.Seek(-1, SeekOrigin.Current);

                byte Value = (byte)BaseStream.ReadByte();

                BaseStream.Seek(-1, SeekOrigin.Current);

                return Value;
            }

            public short ReadInt16()
            {
                return (short)((ReadByte() << 8) | (ReadByte() << 0));
            }

            public int ReadInt32()
            {
                return ((ReadByte() << 24) |
                        (ReadByte() << 16) |
                        (ReadByte() << 8)  |
                        (ReadByte() << 0));
            }
        }

        public static byte[] Decompress(Stream Input)
        {
            BackwardsReader Reader = new BackwardsReader(Input);

            int AdditionalDecLength = Reader.ReadInt32();
            int StartOffset         = Reader.ReadInt32();
            int CompressedLength    = Reader.ReadInt32();

            Input.Seek(12 - StartOffset, SeekOrigin.Current);

            byte[] Dec = new byte[CompressedLength + AdditionalDecLength];

            int DecPos = Dec.Length;

            byte Mask   = 0;
            byte Header = 0;

            while (DecPos > 0)
            {
                if ((Mask >>= 1) == 0)
                {
                    Header = Reader.ReadByte();
                    Mask   = 0x80;
                }

                if ((Header & Mask) == 0)
                {
                    Dec[--DecPos] = Reader.ReadByte();
                }
                else
                {
                    ushort Pair = (ushort)Reader.ReadInt16();

                    int Length   = (Pair >> 12)   + 3;
                    int Position = (Pair & 0xfff) + 3;

                    if (Position - Length >= DecPos)
                    {
                        int SrcPos = DecPos + Position;

                        DecPos -= Length;

                        Buffer.BlockCopy(Dec, SrcPos, Dec, DecPos, Length);
                    }
                    else
                    {
                        while (Length-- > 0)
                        {
                            Dec[--DecPos] = Dec[DecPos + Position + 1];
                        }
                    }
                }
            }

            return Dec;
        }
    }
}