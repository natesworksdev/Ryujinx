namespace Ryujinx.HLE.HOS.Services.Notification
{
    [Service("notif:s")] // 9.0.0+
    class INotificationServicesForSystem : IpcService
    {
#pragma warning disable IDE0060
        public INotificationServicesForSystem(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}
