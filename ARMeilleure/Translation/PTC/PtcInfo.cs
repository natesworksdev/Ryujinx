using ARMeilleure.CodeGen.Unwinding;
using System;
using System.IO;

using static ARMeilleure.Translation.PTC.PtcFormatter;

namespace ARMeilleure.Translation.PTC
{
    class PtcInfo : IDisposable
    {
        public byte[] Code { get; set; }

        public MemoryStream RelocStream      { get; }
        public MemoryStream UnwindInfoStream { get; }

        public int RelocEntriesCount { get; private set; }

        public PtcInfo()
        {
            RelocStream      = new MemoryStream();
            UnwindInfoStream = new MemoryStream();

            RelocEntriesCount = 0;
        }

        public void WriteRelocEntry(RelocEntry relocEntry)
        {
            SerializeStructure(RelocStream, relocEntry);

            RelocEntriesCount++;
        }

        public void WriteUnwindInfo(UnwindInfo unwindInfo)
        {
            SerializeStructure(UnwindInfoStream, (int)unwindInfo.PushEntries.Length);

            foreach (UnwindPushEntry unwindPushEntry in unwindInfo.PushEntries)
            {
                SerializeStructure(UnwindInfoStream, unwindPushEntry);
            }

            SerializeStructure(UnwindInfoStream, (int)unwindInfo.PrologSize);
        }

        public void Dispose()
        {
            RelocStream.Dispose();
            UnwindInfoStream.Dispose();
        }
    }
}