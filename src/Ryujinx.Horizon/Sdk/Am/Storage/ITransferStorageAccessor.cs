using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Am.Storage
{
    interface ITransferStorageAccessor : IServiceObject
    {
        Result GetSize(out long arg0);
        Result GetHandle(out int arg0, out ulong arg1);
    }
}
