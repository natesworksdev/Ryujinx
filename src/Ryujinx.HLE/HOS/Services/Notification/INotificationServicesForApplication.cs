namespace Ryujinx.HLE.HOS.Services.Notification
{
    [Service("notif:a")] // 9.0.0+
    class INotificationServicesForApplication : IpcService
    {
#pragma warning disable IDE0060
        public INotificationServicesForApplication(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}
