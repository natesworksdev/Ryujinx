using Ryujinx.Horizon.Common;

namespace Ryujinx.Horizon.Sdk.Am.Controllers
{
    public interface ILibraryAppletCreator
    {
        Result CreateLibraryApplet();
        Result TerminateAllLibraryApplets();
        Result AreAnyLibraryAppletsLeft();
        Result CreateStorage();
        Result CreateTransferMemoryStorage();
        Result CreateHandleStorage();
    }
}
