using System.IO;

namespace Ryujinx.HLE.Loaders.Executables
{
    class Nro : IExecutable
    {
        public string FilePath { get; private set; }

        public byte[] Text { get; private set; }
        public byte[] Ro   { get; private set; }
        public byte[] Data { get; private set; }

        public int Mod0Offset { get; private set; }
        public int TextOffset { get; private set; }
        public int RoOffset   { get; private set; }
        public int DataOffset { get; private set; }
        public int BssSize    { get; private set; }

        public long SourceAddress { get; private set; }
        public long BssAddress    { get; private set; }

        public Nro(Stream input, string filePath, long sourceAddress = 0, long bssAddress = 0)
        {
            this.FilePath      = filePath;
            this.SourceAddress = sourceAddress;
            this.BssAddress    = bssAddress;

            BinaryReader reader = new BinaryReader(input);

            input.Seek(4, SeekOrigin.Begin);

            int mod0Offset = reader.ReadInt32();
            int padding8   = reader.ReadInt32();
            int paddingc   = reader.ReadInt32();
            int nroMagic   = reader.ReadInt32();
            int unknown14  = reader.ReadInt32();
            int fileSize   = reader.ReadInt32();
            int unknown1C  = reader.ReadInt32();
            int textOffset = reader.ReadInt32();
            int textSize   = reader.ReadInt32();
            int roOffset   = reader.ReadInt32();
            int roSize     = reader.ReadInt32();
            int dataOffset = reader.ReadInt32();
            int dataSize   = reader.ReadInt32();
            int bssSize    = reader.ReadInt32();

            this.Mod0Offset = mod0Offset;
            this.TextOffset = textOffset;
            this.RoOffset   = roOffset;
            this.DataOffset = dataOffset;
            this.BssSize    = bssSize;

            byte[] Read(long position, int size)
            {
                input.Seek(position, SeekOrigin.Begin);

                return reader.ReadBytes(size);
            }

            Text = Read(textOffset, textSize);
            Ro   = Read(roOffset,   roSize);
            Data = Read(dataOffset, dataSize);
        }
    }
}