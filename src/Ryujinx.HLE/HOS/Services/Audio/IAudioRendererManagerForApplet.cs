namespace Ryujinx.HLE.HOS.Services.Audio
{
    [Service("audren:a")]
    sealed class IAudioRendererManagerForApplet : IpcService
    {
        public IAudioRendererManagerForApplet(ServiceCtx context) { }
    }
}