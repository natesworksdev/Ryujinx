using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am.Proxies;
using Ryujinx.Horizon.Sdk.Sf;
using System;

namespace Ryujinx.Horizon.Sdk.Am.Controllers
{
    interface IAllSystemAppletProxiesService : IServiceObject
    {
        Result OpenSystemAppletProxy(out ISystemAppletProxy systemAppletProxy, ulong unknown1, int unknown2, ulong pid);
        Result OpenLibraryAppletProxyOld(out ILibraryAppletProxy libraryAppletProxy, ulong unknown1, int unknown2, ulong pid);
        Result OpenLibraryAppletProxy(out ILibraryAppletProxy libraryAppletProxy, ulong unknown1, int unknown2, ReadOnlySpan<byte> appletAttribute, ulong pid);
        Result OpenOverlayAppletProxy(out IOverlayAppletProxy overlayAppletProxy, ulong unknown1, int unknown2, ulong pid);
        Result OpenSystemApplicationProxy(out IApplicationProxy applicationProxy, ulong unknown1, int unknown2, ulong pid);
        Result CreateSelfLibraryAppletCreatorForDevelop(out ILibraryAppletCreator libraryAppletCreator, ulong unknown, ulong pid);
        Result GetSystemAppletControllerForDebug(out ISystemAppletControllerForDebug systemAppletControllerForDebug);
        Result GetDebugFunctions(out IDebugFunctions debugFunctions);
    }
}
