namespace Ryujinx.HLE.HOS.Services.Time
{
    [Service("time:al")] // 9.0.0+
    class IAlarmService : IpcService
    {
#pragma warning disable IDE0060
        public IAlarmService(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}
