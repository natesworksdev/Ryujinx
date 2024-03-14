using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Account;
using Ryujinx.Horizon.Sdk.Am;
using Ryujinx.Horizon.Sdk.Am.Controllers;
using Ryujinx.Horizon.Sdk.Am.Storage;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using System;
using ApplicationId = Ryujinx.Horizon.Sdk.Ncm.ApplicationId;


namespace Ryujinx.Horizon.Am.Ipc.Controllers
{
    partial class ApplicationAccessor : IApplicationAccessor
    {
        private readonly ApplicationId _applicationId;

        public ApplicationAccessor(ApplicationId applicationId)
        {
            _applicationId = applicationId;
        }

        [CmifCommand(0)]
        public Result GetAppletStateChangedEvent([CopyHandle] out int arg0)
        {
            arg0 = 0;
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result IsCompleted(out bool arg0)
        {
            arg0 = false;
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(10)]
        public Result Start()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(20)]
        public Result RequestExit()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(25)]
        public Result Terminate()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(30)]
        public Result GetResult()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(101)]
        public Result RequestForApplicationToGetForeground()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(110)]
        public Result TerminateAllLibraryApplets()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(111)]
        public Result AreAnyLibraryAppletsLeft(out bool arg0)
        {
            arg0 = false;
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(112)]
        public Result GetCurrentLibraryApplet(out IAppletAccessor arg0)
        {
            arg0 = new AppletAccessor();
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(120)]
        public Result GetApplicationId(out ApplicationId arg0)
        {
            arg0 = _applicationId;

            return Result.Success;
        }

        [CmifCommand(121)]
        public Result PushLaunchParameter(uint arg0, IStorage arg1)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(122)]
        public Result GetApplicationControlProperty([Buffer(HipcBufferFlags.Out | HipcBufferFlags.AutoSelect)] Span<byte> arg0)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(123)]
        public Result GetApplicationLaunchProperty([Buffer(HipcBufferFlags.Out | HipcBufferFlags.AutoSelect)] Span<byte> arg0)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(124)]
        public Result GetApplicationLaunchRequestInfo(out ApplicationLaunchRequestInfo arg0)
        {
            arg0 = new ApplicationLaunchRequestInfo();
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(130)]
        public Result SetUsers(bool arg0, ReadOnlySpan<Uid> arg1)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(131)]
        public Result CheckRightsEnvironmentAvailable(out bool arg0)
        {
            arg0 = false;
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(132)]
        public Result GetNsRightsEnvironmentHandle(out ulong arg0)
        {
            arg0 = 0;
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(140)]
        public Result GetDesirableUids(out int arg0, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.AutoSelect)] Span<Uid> arg1)
        {
            arg0 = 0;
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(150)]
        public Result ReportApplicationExitTimeout()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(160)]
        public Result SetApplicationAttribute(in ApplicationAttribute arg0)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(170)]
        public Result HasSaveDataAccessPermission(out bool arg0, ApplicationId arg1)
        {
            arg0 = false;
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(180)]
        public Result PushToFriendInvitationStorageChannel(IStorage arg0)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(190)]
        public Result PushToNotificationStorageChannel(IStorage arg0)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(200)]
        public Result RequestApplicationSoftReset()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(201)]
        public Result RestartApplicationTimer()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }
    }
}
