namespace Ryujinx.HLE.HOS.Services.Audio
{
    [Service("auddev")] // 6.0.0+
    sealed class IAudioSnoopManager : IpcService
    {
        public IAudioSnoopManager(ServiceCtx context) { }
    }
}