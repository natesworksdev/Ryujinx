using System.IO;

namespace ARMeilleure.Translation.AOT
{
    class AotInfo
    {
        private readonly BinaryWriter _relocWriter;

        public MemoryStream CodeStream  { get; }
        public MemoryStream RelocStream { get; }

        public int RelocEntriesCount { get; private set; }

        public AotInfo()
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
            _relocWriter.Write(relocEntry.Position);
            _relocWriter.Write(relocEntry.Name);

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