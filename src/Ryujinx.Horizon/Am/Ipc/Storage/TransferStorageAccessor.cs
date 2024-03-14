using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am.Storage;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Am.Ipc.Storage
{
    partial class TransferStorageAccessor : ITransferStorageAccessor
    {
        private readonly Storage _storage;

        public TransferStorageAccessor(Storage storage)
        {
            _storage = storage;
        }

        [CmifCommand(0)]
        public Result GetSize(out long size)
        {
            size = _storage.Data.Length;

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result GetHandle([CopyHandle] out int handle, out ulong size)
        {
            handle = 0;
            size = 0;
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }
    }
}
