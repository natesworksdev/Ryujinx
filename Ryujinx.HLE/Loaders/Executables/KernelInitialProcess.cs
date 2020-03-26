using Ryujinx.HLE.Loaders.Compression;
using System.IO;
using LibHac;
using LibHac.Fs;

namespace Ryujinx.HLE.Loaders.Executables
{
    class KernelInitialProcess : IExecutable
    {
        Kip kip;
        public string Name => kip.Header.Name;

        public ulong TitleId => kip.Header.TitleId;

        public int ProcessCategory => kip.Header.ProcessCategory;

        public byte MainThreadPriority => kip.Header.MainThreadPriority;
        public byte DefaultProcessorId => kip.Header.DefaultCore;

        public bool Is64Bits => (kip.Header.Flags & 0x08) != 0;
        public bool Addr39Bits => (kip.Header.Flags & 0x10) != 0;
        public bool IsService => (kip.Header.Flags & 0x20) != 0;


        public byte[] Text { get; private set; }
        public byte[] Ro { get; private set; }
        public byte[] Data { get; private set; }

        public int TextOffset => kip.Header.Sections[0].OutOffset;
        public int RoOffset => kip.Header.Sections[1].OutOffset;
        public int DataOffset => kip.Header.Sections[2].OutOffset;
        public int BssOffset => kip.Header.Sections[3].OutOffset;
        public int BssSize => kip.Header.Sections[3].DecompressedSize;

        public int MainThreadStackSize => kip.Header.Sections[1].Attribute;
        public int[] Capabilities { get; set; }

        private struct SegmentHeader
        {
            public int Offset { get; private set; }
            public int DecompressedSize { get; private set; }
            public int CompressedSize { get; private set; }
            public int Attribute { get; private set; }

            public SegmentHeader(
                int offset,
                int decompressedSize,
                int compressedSize,
                int attribute)
            {
                Offset = offset;
                DecompressedSize = decompressedSize;
                CompressedSize = compressedSize;
                Attribute = attribute;
            }
        }

        public KernelInitialProcess(IStorage inStorage)
        {
            kip = new Kip(inStorage);
            Capabilities = new int[32];

            for (int index = 0; index < Capabilities.Length; index++)
            {
                Capabilities[index] = System.BitConverter.ToInt32(kip.Header.Capabilities, index * 4);
            }


            Text = kip.DecompressSection(0);
            Ro = kip.DecompressSection(1);
            Data = kip.DecompressSection(2);
        }

    }
}