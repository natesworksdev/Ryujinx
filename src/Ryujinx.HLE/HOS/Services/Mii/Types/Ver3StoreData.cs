using Ryujinx.Common.Memory;
using Ryujinx.Common.Utilities;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Mii.Types
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = Size)]
    struct Ver3StoreData
    {
        public const int Size = 0x60;

        private Array96<byte> _storage;

        [UnscopedRef]
        public Span<byte> Storage => _storage;

        // TODO: define all getters/setters
    }
}
