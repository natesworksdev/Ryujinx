using System;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Applets.Error
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct ApplicationErrorArg
    {
        public uint       ErrorNumber;
        public ulong      LanguageCode;
        public TextStruct MessageText;
        public TextStruct DetailsText;

        [StructLayout(LayoutKind.Sequential, Size = 0x800)]
        public struct TextStruct
        {
            private byte element;

            public Span<byte> ToSpan() => MemoryMarshal.CreateSpan(ref element, 0x800);
        }
    }
} 