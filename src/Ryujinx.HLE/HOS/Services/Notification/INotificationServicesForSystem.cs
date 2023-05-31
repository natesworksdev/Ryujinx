namespace Ryujinx.HLE.HOS.Services.Notification
{
    [Service("notif:s")] // 9.0.0+
    sealed class INotificationServicesForSystem : IpcService
    {
        public INotificationServicesForSystem(ServiceCtx context) { }
    }
}