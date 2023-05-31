namespace Ryujinx.HLE.HOS.Services.Audio
{
    [Service("audout:a")]
    sealed class IAudioOutManagerForApplet : IpcService
    {
        public IAudioOutManagerForApplet(ServiceCtx context) { }
    }
}