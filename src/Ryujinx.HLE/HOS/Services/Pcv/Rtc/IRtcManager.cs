namespace Ryujinx.HLE.HOS.Services.Pcv.Rtc
{
    [Service("rtc")] // 8.0.0+
    sealed class IRtcManager : IpcService
    {
        public IRtcManager(ServiceCtx context) { }
    }
}