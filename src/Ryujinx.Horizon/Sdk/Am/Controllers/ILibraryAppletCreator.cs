using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am.Storage;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Am.Controllers
{
    interface ILibraryAppletCreator : IServiceObject
    {
        Result CreateLibraryApplet(out ILibraryAppletAccessor arg0, uint arg1, uint arg2);
        Result TerminateAllLibraryApplets();
        Result AreAnyLibraryAppletsLeft(out bool arg0);
        Result CreateStorage(out IStorage arg0, long arg1);
        Result CreateTransferMemoryStorage(out IStorage arg0, int arg1, long arg2, bool arg3);
        Result CreateHandleStorage(out IStorage arg0, int arg1, long arg2);
    }
}
