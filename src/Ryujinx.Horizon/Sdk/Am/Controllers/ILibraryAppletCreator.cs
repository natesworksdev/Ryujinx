using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Am.Controllers
{
    interface ILibraryAppletCreator : IServiceObject
    {
        Result CreateLibraryApplet();
        Result TerminateAllLibraryApplets();
        Result AreAnyLibraryAppletsLeft();
        Result CreateStorage();
        Result CreateTransferMemoryStorage();
        Result CreateHandleStorage();
    }
}
