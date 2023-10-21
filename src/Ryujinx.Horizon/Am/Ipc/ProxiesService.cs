using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Am.Ipc.Proxies;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am.Controllers;
using Ryujinx.Horizon.Sdk.Am.Proxies;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using System;

namespace Ryujinx.Horizon.Am.Ipc
{
    partial class ProxiesService : IAllSystemAppletProxiesService
    {
        [CmifCommand(100)]
        public Result OpenSystemAppletProxy(out ISystemAppletProxy systemAppletProxy, ulong unknown1, [CopyHandle] int unknown2, [ClientProcessId] ulong pid)
        {
            systemAppletProxy = new SystemAppletProxy();

            return Result.Success;
        }

        [CmifCommand(200)]
        public Result OpenLibraryAppletProxyOld(out ILibraryAppletProxy libraryAppletProxy, ulong unknown1, [CopyHandle] int unknown2, [ClientProcessId] ulong pid)
        {
            OpenLibraryAppletProxy(out libraryAppletProxy, unknown1, unknown2, new byte[0x80], pid);

            return Result.Success;
        }

        [CmifCommand(201)]
        public Result OpenLibraryAppletProxy(out ILibraryAppletProxy libraryAppletProxy, ulong unknown1, [CopyHandle] int unknown2, [Buffer(HipcBufferFlags.In | HipcBufferFlags.MapAlias, 0x80)] ReadOnlySpan<byte> appletAttribute, [ClientProcessId] ulong pid)
        {
            libraryAppletProxy = new LibraryAppletProxy();

            return Result.Success;
        }

        [CmifCommand(300)]
        public Result OpenOverlayAppletProxy(out IOverlayAppletProxy overlayAppletProxy, ulong unknown1, [CopyHandle] int unknown2, [ClientProcessId] ulong pid)
        {
            overlayAppletProxy = new OverlayAppletProxy();

            return Result.Success;
        }

        [CmifCommand(350)]
        public Result OpenSystemApplicationProxy(out IApplicationProxy applicationProxy, ulong unknown1, [CopyHandle] int unknown2, [ClientProcessId] ulong pid)
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
