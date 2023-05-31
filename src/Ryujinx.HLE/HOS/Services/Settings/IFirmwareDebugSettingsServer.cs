namespace Ryujinx.HLE.HOS.Services.Settings
{
    [Service("set:fd")]
    sealed class IFirmwareDebugSettingsServer : IpcService
    {
        public IFirmwareDebugSettingsServer(ServiceCtx context) { }
    }
}