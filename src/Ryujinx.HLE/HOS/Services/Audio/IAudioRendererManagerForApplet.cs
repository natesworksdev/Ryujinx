namespace Ryujinx.HLE.HOS.Services.Audio
{
    [Service("audren:a")]
    class IAudioRendererManagerForApplet : IpcService
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public IAudioRendererManagerForApplet(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}