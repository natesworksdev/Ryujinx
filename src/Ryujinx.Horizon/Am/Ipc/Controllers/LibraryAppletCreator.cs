using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am;
using Ryujinx.Horizon.Sdk.Am.Controllers;
using Ryujinx.Horizon.Sdk.Am.Storage;
using Ryujinx.Horizon.Sdk.Sf;
using System.Linq;

namespace Ryujinx.Horizon.Am.Ipc.Controllers
{
    partial class LibraryAppletCreator : ILibraryAppletCreator
    {
        [CmifCommand(0)]
        public Result CreateLibraryApplet(out ILibraryAppletAccessor libraryAppletAccessor, uint appletId, uint libraryAppletMode)
        {
            libraryAppletAccessor = new LibraryAppletAccessor((AppletId)appletId);

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result TerminateAllLibraryApplets()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(2)]
        public Result AreAnyLibraryAppletsLeft(out bool arg0)
        {
            arg0 = true;
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(10)]
        public Result CreateStorage(out IStorage storage, long size)
        {
            storage = new Storage.Storage(Enumerable.Repeat((byte)0, (int)size).ToArray());

            return Result.Success;
        }

        [CmifCommand(11)]
        public Result CreateTransferMemoryStorage(out IStorage storage, int handle, long size, bool writeable)
        {
            storage = new Storage.Storage(Enumerable.Repeat((byte)0, (int)size).ToArray(), writeable);
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(12)]
        public Result CreateHandleStorage(out IStorage storage, int handle, long size)
        {
            storage = new Storage.Storage(Enumerable.Repeat((byte)0, (int)size).ToArray());
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }
    }
}
