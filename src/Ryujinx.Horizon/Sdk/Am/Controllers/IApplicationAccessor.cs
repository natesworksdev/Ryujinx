using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Account;
using Ryujinx.Horizon.Sdk.Am.Storage;
using ApplicationId = Ryujinx.Horizon.Sdk.Ncm.ApplicationId;
using System;

namespace Ryujinx.Horizon.Sdk.Am.Controllers
{
    interface IApplicationAccessor : IAppletAccessor
    {
        Result RequestForApplicationToGetForeground();
        Result TerminateAllLibraryApplets();
        Result AreAnyLibraryAppletsLeft(out bool arg0);
        Result GetCurrentLibraryApplet(out IAppletAccessor arg0);
        Result GetApplicationId(out ApplicationId arg0);
        Result PushLaunchParameter(uint arg0, IStorage arg1);
        Result GetApplicationControlProperty(Span<byte> arg0);
        Result GetApplicationLaunchProperty(Span<byte> arg0);
        Result GetApplicationLaunchRequestInfo(out ApplicationLaunchRequestInfo arg0);
        Result SetUsers(bool arg0, ReadOnlySpan<Uid> arg1);
        Result CheckRightsEnvironmentAvailable(out bool arg0);
        Result GetNsRightsEnvironmentHandle(out ulong arg0);
        Result GetDesirableUids(out int arg0, Span<Uid> arg1);
        Result ReportApplicationExitTimeout();
        Result SetApplicationAttribute(in ApplicationAttribute arg0);
        Result HasSaveDataAccessPermission(out bool arg0, ApplicationId arg1);
        Result PushToFriendInvitationStorageChannel(IStorage arg0);
        Result PushToNotificationStorageChannel(IStorage arg0);
        Result RequestApplicationSoftReset();
        Result RestartApplicationTimer();
    }
}
