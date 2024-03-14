using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am.Storage;
using Ryujinx.Horizon.Sdk.Sf;
using System.Collections.Generic;

namespace Ryujinx.Horizon.Am.Ipc.Storage
{
    // Might not be used outside of debug software
    partial class StorageChannel : IStorageChannel
    {
        private readonly Stack<IStorage> _storages;

        public StorageChannel()
        {
            _storages = new Stack<IStorage>();
        }

        [CmifCommand(0)]
        public Result Push(IStorage storage)
        {
            _storages.Push(storage);

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result Unpop(IStorage storage)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(2)]
        public Result Pop(out IStorage storage)
        {
            storage = _storages.Pop();

            return Result.Success;
        }

        [CmifCommand(3)]
        public Result GetPopEventHandle([CopyHandle] out int handle)
        {
            handle = 0;
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(4)]
        public Result Clear()
        {
            _storages.Clear();

            return Result.Success;
        }
    }
}
