using Ryujinx.Horizon.Common;

namespace Ryujinx.Horizon.Sdk.Am
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
