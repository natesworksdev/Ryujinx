using Ryujinx.Common.Memory;
using Ryujinx.Common.Utilities;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Mii.Types
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = Size)]
    struct Ver3StoreData
    {
        public const int Size = 0x60;

        private Array64<byte> _storage;
        private Array32<byte> _storage2;

        public Span<byte> Storage => SpanHelpers.CreateSpan(ref MemoryMarshal.GetReference(_storage.AsSpan()), Size);

        // TODO: define all getters/setters
    }
}
