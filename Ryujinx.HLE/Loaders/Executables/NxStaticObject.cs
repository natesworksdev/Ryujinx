using Ryujinx.HLE.Loaders.Compression;
using System;
using System.IO;
using LibHac.Fs;
using LibHac;

namespace Ryujinx.HLE.Loaders.Executables
{
    class NxStaticObject : Nso, IExecutable
    {

        public byte[] Text { get; private set; }
        public byte[] Ro { get; private set; }
        public byte[] Data { get; private set; }

        public int TextOffset => (int)Sections[0].MemoryOffset;
        public int RoOffset => (int)Sections[1].MemoryOffset;
        public int DataOffset => (int)Sections[2].MemoryOffset;
        public int BssOffset => DataOffset + Data.Length;

        public new int BssSize => (int)base.BssSize;

        public NxStaticObject(IStorage inStorage) : base(inStorage)
        {
            Text = Sections[0].DecompressSection();
            Ro = Sections[1].DecompressSection();
            Data =  Sections[2].DecompressSection();
        }

    }
}