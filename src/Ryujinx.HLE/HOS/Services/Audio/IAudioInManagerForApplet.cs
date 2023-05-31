namespace Ryujinx.HLE.HOS.Services.Audio
{
    [Service("audin:a")]
    sealed class IAudioInManagerForApplet : IpcService
    {
        public IAudioInManagerForApplet(ServiceCtx context) { }
    }
}