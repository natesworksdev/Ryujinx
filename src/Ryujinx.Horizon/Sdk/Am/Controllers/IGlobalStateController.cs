using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Sdk.Am.Controllers
{
    interface IGlobalStateController : IServiceObject
    {
        Result RequestToEnterSleep();
        Result EnterSleep();
        Result StartSleepSequence(bool arg0);
        Result StartShutdownSequence();
        Result StartRebootSequence();
        Result IsAutoPowerDownRequested(out bool arg0);
        Result LoadAndApplyIdlePolicySettings();
        Result NotifyCecSettingsChanged();
        Result SetDefaultHomeButtonLongPressTime(long arg0);
        Result UpdateDefaultDisplayResolution();
        Result ShouldSleepOnBoot(out bool arg0);
        Result GetHdcpAuthenticationFailedEvent(out int arg0);
        Result OpenCradleFirmwareUpdater(out ICradleFirmwareUpdater arg0);
    }
}
