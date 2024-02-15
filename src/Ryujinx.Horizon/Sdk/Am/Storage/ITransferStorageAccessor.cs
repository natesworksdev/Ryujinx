using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Am.Storage
{
    interface ITransferStorageAccessor : IServiceObject
    {
        Result GetSize(out long size);
        Result GetHandle(out int arg0, out ulong arg1);
    }
}
