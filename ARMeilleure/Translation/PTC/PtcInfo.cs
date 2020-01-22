using System;
using System.IO;

namespace ARMeilleure.Translation.PTC
{
    sealed class PtcInfo : IDisposable
    {
        private readonly BinaryWriter _relocWriter;

        public MemoryStream CodeStream  { get; }
        public MemoryStream RelocStream { get; }

        public int RelocEntriesCount { get; private set; }

        public PtcInfo()
        {
            CodeStream  = new MemoryStream();
            RelocStream = new MemoryStream();

            _relocWriter = new BinaryWriter(RelocStream, EncodingCache.UTF8NoBOM, true);

            RelocEntriesCount = 0;
        }

        public void WriteCode(MemoryStream codeStream)
        {
            codeStream.WriteTo(CodeStream);
        }

        public void WriteRelocEntry(RelocEntry relocEntry)
        {
            _relocWriter.Write((int)relocEntry.Position);
            _relocWriter.Write((int)relocEntry.Index);

            RelocEntriesCount++;
        }

        public void Dispose()
        {
            _relocWriter.Dispose();

            CodeStream. Dispose();
            RelocStream.Dispose();
        }
    }
}
