namespace Ryujinx.HLE.HOS.Services.Time
{
    [Service("time:al")] // 9.0.0+
    sealed class IAlarmService : IpcService
    {
        public IAlarmService(ServiceCtx context) { }
    }
}