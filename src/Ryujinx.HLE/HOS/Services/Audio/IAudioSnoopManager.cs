namespace Ryujinx.HLE.HOS.Services.Audio
{
    [Service("auddev")] // 6.0.0+
    class IAudioSnoopManager : IpcService
    {
#pragma warning disable IDE0060
        public IAudioSnoopManager(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}
