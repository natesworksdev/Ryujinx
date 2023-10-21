using LibHac.Diag;
using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Am.Ipc.Proxies;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Am.Ipc
{
    partial class ProxiesService : IAllSystemAppletProxiesService
    {
        [CmifCommand(100)]
        public Result OpenSystemAppletProxy(out ISystemAppletProxy systemAppletProxy, [ClientProcessId] ulong pid)
        {
            systemAppletProxy = new SystemAppletProxy();

            return Result.Success;
        }

        [CmifCommand(200)]
        public Result OpenLibraryAppletProxyOld(out ILibraryAppletProxy libraryAppletProxy, [ClientProcessId] ulong pid)
        {
            OpenLibraryAppletProxy(out libraryAppletProxy, pid);

            return Result.Success;
        }

        [CmifCommand(201)]
        public Result OpenLibraryAppletProxy(out ILibraryAppletProxy libraryAppletProxy, [ClientProcessId] ulong pid)
        {
            libraryAppletProxy = new LibraryAppletProxy();

            return Result.Success;
        }

        [CmifCommand(300)]
        public Result OpenOverlayAppletProxy(out IOverlayAppletProxy overlayAppletProxy, [ClientProcessId] ulong pid)
        {
            overlayAppletProxy = new OverlayAppletProxy();

            return Result.Success;
        }

        [CmifCommand(350)]
        public Result OpenSystemApplicationProxy(out IApplicationProxy applicationProxy, [ClientProcessId] ulong pid)
        {
            applicationProxy = new ApplicationProxy();

            return Result.Success;
        }

        [CmifCommand(400)]
        public Result CreateSelfLibraryAppletCreatorForDevelop()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(410)]
        public Result GetSystemAppletControllerForDebug()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(1000)]
        public Result GetDebugFunctions()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }
    }
}
