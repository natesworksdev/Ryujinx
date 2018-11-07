using Ryujinx.HLE.Loaders.Compression;
using System.IO;

namespace Ryujinx.HLE.Loaders.Executables
{
    class KernelInitialProcess
    {
        public string Name { get; private set; }

        public long TitleId { get; private set; }

        public int ProcessCategory { get; private set; }

        public byte MainThreadPriority { get; private set; }
        public byte DefaultProcessorId { get; private set; }

        public byte[] Text { get; private set; }
        public byte[] RO   { get; private set; }
        public byte[] Data { get; private set; }

        public int TextOffset { get; private set; }
        public int ROOffset   { get; private set; }
        public int DataOffset { get; private set; }
        public int BssOffset  { get; private set; }
        public int BssSize    { get; private set; }

        private struct SegmentHeader
        {
            public int Offset           { get; private set; }
            public int DecompressedSize { get; private set; }
            public int CompressedSize   { get; private set; }
            public int Attribute        { get; private set; }

            public SegmentHeader(
                int Offset,
                int DecompressedSize,
                int CompressedSize,
                int Attribute)
            {
                this.Offset           = Offset;
                this.DecompressedSize = DecompressedSize;
                this.CompressedSize   = CompressedSize;
                this.Attribute        = Attribute;
            }
        }

        public KernelInitialProcess(Stream Input)
        {
            BinaryReader Reader = new BinaryReader(Input);

            string Magic = ReadString(Reader, 4);

            if (Magic != "KIP1")
            {

            }

            Name = ReadString(Reader, 12);

            TitleId = Reader.ReadInt64();

            ProcessCategory = Reader.ReadInt32();

            MainThreadPriority = Reader.ReadByte();
            DefaultProcessorId = Reader.ReadByte();

            byte Reserved = Reader.ReadByte();
            byte Flags    = Reader.ReadByte();

            SegmentHeader[] Segments = new SegmentHeader[6];

            for (int Index = 0; Index < Segments.Length; Index++)
            {
                Segments[Index] = new SegmentHeader(
                    Reader.ReadInt32(),
                    Reader.ReadInt32(),
                    Reader.ReadInt32(),
                    Reader.ReadInt32());
            }

            Reader.ReadBytes(0x20);

            Text = ReadSegment(Segments[0], Input);
            RO   = ReadSegment(Segments[1], Input);
            Data = ReadSegment(Segments[2], Input);

            TextOffset = Segments[0].Offset;
            ROOffset   = Segments[1].Offset;
            DataOffset = Segments[2].Offset;
            BssOffset  = Segments[3].Offset;
            BssSize    = Segments[3].DecompressedSize;
        }

        private byte[] ReadSegment(SegmentHeader Header, Stream Input)
        {
            long End = Input.Position + Header.CompressedSize;

            Input.Seek(End, SeekOrigin.Begin);

            byte[] Data = BackwardsLz.Decompress(Input);

            Input.Seek(End, SeekOrigin.Begin);

            return Data;
        }

        private static string ReadString(BinaryReader Reader, int MaxSize)
        {
            string Value = string.Empty;

            for (int Index = 0; Index < MaxSize; Index++)
            {
                char Chr = (char)Reader.ReadByte();

                if (Chr == '\0')
                {
                    Reader.BaseStream.Seek(MaxSize - Index - 1, SeekOrigin.Current);

                    break;
                }

                Value += Chr;
            }

            return Value;
        }
    }
}