using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;
using System;

namespace Ryujinx.Horizon.Sdk.Am.Storage
{
    interface IStorageAccessor : IServiceObject
    {
        Result GetSize(out long arg0);
        Result Write(long arg0, ReadOnlySpan<byte> arg1);
        Result Read(long arg0, Span<byte> arg1);
    }
}
