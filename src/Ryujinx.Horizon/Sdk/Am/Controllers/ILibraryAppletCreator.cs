using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am.Storage;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Am.Controllers
{
    interface ILibraryAppletCreator : IServiceObject
    {
        Result CreateLibraryApplet(out ILibraryAppletAccessor libraryAppletAccessor, uint appletId, uint libraryAppletMode);
        Result TerminateAllLibraryApplets();
        Result AreAnyLibraryAppletsLeft(out bool arg0);
        Result CreateStorage(out IStorage storage, long size);
        Result CreateTransferMemoryStorage(out IStorage storage, int handle, long size, bool writeable);
        Result CreateHandleStorage(out IStorage storage, int handle, long size);
    }
}
