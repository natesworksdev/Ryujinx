using Ryujinx.HLE.Loaders.Compression;
using System;
using System.IO;
using LibHac.Fs;
using LibHac;

namespace Ryujinx.HLE.Loaders.Executables
{
    class NxStaticObject : IExecutable
    {
        Nso nso;

        public byte[] Text { get; private set; }
        public byte[] Ro   { get; private set; }
        public byte[] Data { get; private set; }

        public int TextOffset => (int)nso.Sections[0].MemoryOffset;
        public int RoOffset   => (int)nso.Sections[1].MemoryOffset;
        public int DataOffset => (int)nso.Sections[2].MemoryOffset;
        public int BssOffset => DataOffset + Data.Length;
        public int BssSize => (int)nso.BssSize;

        [Flags]
        private enum NsoFlags
        {
            IsTextCompressed = 1 << 0,
            IsRoCompressed   = 1 << 1,
            IsDataCompressed = 1 << 2,
            HasTextHash      = 1 << 3,
            HasRoHash        = 1 << 4,
            HasDataHash      = 1 << 5
        }

        public NxStaticObject(IStorage inStorage)
        {
            nso = new Nso(inStorage);
            Text = nso.Sections[0].DecompressSection();
            Ro = nso.Sections[1].DecompressSection();
            Data = nso.Sections[2].DecompressSection();
        }
        
    }
}