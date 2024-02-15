using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am.Storage;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Am.Ipc.Storage
{
    partial class Storage : IStorage
    {
        [CmifCommand(0)]
        public Result Open(out IStorageAccessor storageAccessor)
        {
            storageAccessor = new StorageAccessor();
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result OpenTransferStorage(out ITransferStorageAccessor transferStorageAccessor)
        {
            transferStorageAccessor = new TransferStorageAccessor();
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        // TODO: Get CMD No.
        public Result GetAndInvalidate(out IStorage storage)
        {
            storage = new Storage();
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }
    }
}
