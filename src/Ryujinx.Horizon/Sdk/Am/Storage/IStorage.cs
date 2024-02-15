using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Am.Storage
{
    interface IStorage : IServiceObject
    {
        Result Open(out IStorageAccessor storageAccessor);
        Result OpenTransferStorage(out ITransferStorageAccessor transferStorageAccessor);
    }
}
