using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am.Proxies;

namespace Ryujinx.Horizon.Sdk.Am.Controllers
{
    public interface IAllSystemAppletProxiesService
    {
        Result OpenSystemAppletProxy(out ISystemAppletProxy systemAppletProxy, ulong pid);
        Result OpenLibraryAppletProxyOld(out ILibraryAppletProxy libraryAppletProxy, ulong pid);
        Result OpenLibraryAppletProxy(out ILibraryAppletProxy libraryAppletProxy, ulong pid);
        Result OpenOverlayAppletProxy(out IOverlayAppletProxy overlayAppletProxy, ulong pid);
        Result OpenSystemApplicationProxy(out IApplicationProxy applicationProxy, ulong pid);
        Result CreateSelfLibraryAppletCreatorForDevelop();
        Result GetSystemAppletControllerForDebug();
        Result GetDebugFunctions();
    }
}
