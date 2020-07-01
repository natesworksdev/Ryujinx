using LibHac;
using LibHac.Fs;
using LibHac.FsSystem.Save;
using LibHac.Loader;
using System.IO;

namespace Ryujinx.HLE.Loaders.Executables
{
    class KipExecutable : IExecutable
    {
        public byte[] Text { get; }
        public byte[] Ro { get; }
        public byte[] Data { get; }

        public int TextOffset => Header.Segments[0].MemoryOffset;
        public int RoOffset => Header.Segments[1].MemoryOffset;
        public int DataOffset => Header.Segments[2].MemoryOffset;
        public int BssOffset => Header.Segments[3].MemoryOffset;
        public int BssSize => Header.Segments[3].Size;

        public LibHac.Loader.KipHeader Header { get; }

        public int[] Capabilities { get; }

        public KipExecutable(IStorage inStorage)
        {
            KipReader reader = new KipReader();
            reader.Initialize(inStorage).ThrowIfFailure();

            Capabilities = new int[32];

            for (int index = 0; index < Capabilities.Length; index++)
            {
                Capabilities[index] = (int)Header.Capabilities[index];
            }

            Text = DecompressSection(reader, KipReader.SegmentType.Text);
            Ro = DecompressSection(reader, KipReader.SegmentType.Ro);
            Data = DecompressSection(reader, KipReader.SegmentType.Data);
        }

        private static byte[] DecompressSection(KipReader reader, KipReader.SegmentType segmentType)
        {
            reader.GetSegmentSize(segmentType, out int uncompressedSize).ThrowIfFailure();

            byte[] result = new byte[uncompressedSize];

            reader.ReadSegment(segmentType, result).ThrowIfFailure();

            return result;
        }
    }
}