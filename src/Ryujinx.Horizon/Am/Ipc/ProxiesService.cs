using Ryujinx.Horizon.Am.Ipc.Controllers;
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
        public Result CreateSelfLibraryAppletCreatorForDevelop(out ILibraryAppletCreator libraryAppletCreator, ulong unknown, [ClientProcessId] ulong pid)
        {
            libraryAppletCreator = new LibraryAppletCreator();

            return Result.Success;
        }

        [CmifCommand(410)]
        public Result GetSystemAppletControllerForDebug(out ISystemAppletControllerForDebug systemAppletControllerForDebug)
        {
            systemAppletControllerForDebug = new SystemAppletControllerForDebug();

            return Result.Success;
        }

        [CmifCommand(1000)]
        public Result GetDebugFunctions(out IDebugFunctions debugFunctions)
        {
            debugFunctions = new DebugFunctions();

            return Result.Success;
        }
    }
}
