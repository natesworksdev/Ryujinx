using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;
using System;

namespace Ryujinx.Horizon.Sdk.Am.Storage
{
    interface IStorageAccessor : IServiceObject
    {
        Result GetSize(out long size);
        Result Write(long offset, ReadOnlySpan<byte> span);
        Result Read(long offset, Span<byte> span);
    }
}
