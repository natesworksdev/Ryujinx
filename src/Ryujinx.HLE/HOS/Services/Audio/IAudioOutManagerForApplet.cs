namespace Ryujinx.HLE.HOS.Services.Audio
{
    [Service("audout:a")]
    class IAudioOutManagerForApplet : IpcService
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public IAudioOutManagerForApplet(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}