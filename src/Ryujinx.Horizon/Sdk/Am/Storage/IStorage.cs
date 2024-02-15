using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Am.Storage
{
    interface IStorage : IServiceObject
    {
        Result Open(out IStorageAccessor arg0);
        Result OpenTransferStorage(out ITransferStorageAccessor arg0);
        Result GetAndInvalidate(out IStorage arg0);
    }
}
