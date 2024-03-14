using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am.Storage;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Am.Ipc.Storage
{
    partial class Storage : IStorage
    {
        public bool IsReadOnly { get; private set; }
        public byte[] Data { get; private set; }

        public Storage(byte[] data, bool isReadOnly = false)
        {
            IsReadOnly = isReadOnly;
            Data = data;
        }

        // TODO: Something for this...
        public Storage(IStorage handle)
        {

        }

        [CmifCommand(0)]
        public Result Open(out IStorageAccessor storageAccessor)
        {
            storageAccessor = new StorageAccessor(this);

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result OpenTransferStorage(out ITransferStorageAccessor transferStorageAccessor)
        {
            transferStorageAccessor = new TransferStorageAccessor(this);

            return Result.Success;
        }
    }
}
