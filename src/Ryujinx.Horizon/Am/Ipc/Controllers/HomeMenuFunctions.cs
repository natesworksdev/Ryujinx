using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Am.Ipc.Storage;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Account;
using Ryujinx.Horizon.Sdk.Am.Controllers;
using Ryujinx.Horizon.Sdk.Am.Storage;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using System;
using ApplicationId = Ryujinx.Horizon.Sdk.Ncm.ApplicationId;

namespace Ryujinx.Horizon.Am.Ipc.Controllers
{
    partial class HomeMenuFunctions : IHomeMenuFunctions
    {
        [CmifCommand(10)]
        public Result RequestToGetForeground()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(11)]
        public Result LockForeground()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(12)]
        public Result UnlockForeground()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(20)]
        public Result PopFromGeneralChannel(out IStorage arg0)
        {
            arg0 = new Storage.Storage([]);
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(21)]
        public Result GetPopFromGeneralChannelEvent([CopyHandle] out int arg0)
        {
            arg0 = 0;
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(30)]
        public Result GetHomeButtonWriterLockAccessor(out ILockAccessor arg0)
        {
            arg0 = new LockAccessor();
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(31)]
        public Result GetWriterLockAccessorEx(out ILockAccessor arg0, int arg1)
        {
            arg0 = new LockAccessor();
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(40)]
        public Result IsSleepEnabled(out bool arg0)
        {
            arg0 = false;
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(41)]
        public Result IsRebootEnabled(out bool arg0)
        {
            arg0 = false;
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(50)]
        public Result LaunchSystemApplet()
        {
            // Wraps ns LaunchSystemApplet
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(51)]
        public Result LaunchStarter()
        {
            // Wraps ns LaunchLibraryApplet with a ProgramId from global state.
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(100)]
        public Result PopRequestLaunchApplicationForDebug(out ApplicationId arg0, out int arg1, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias)] Span<Uid> arg2)
        {
            throw new NotImplementedException();
        }

        [CmifCommand(110)]
        public Result IsForceTerminateApplicationDisabledForDebug(out bool arg0)
        {
            arg0 = false;
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(200)]
        public Result LaunchDevMenu()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(1000)]
        public Result SetLastApplicationExitReason(int arg0)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }
    }
}
