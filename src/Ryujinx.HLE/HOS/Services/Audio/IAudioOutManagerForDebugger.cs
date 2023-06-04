namespace Ryujinx.HLE.HOS.Services.Audio
{
    [Service("audout:d")]
    class IAudioOutManagerForDebugger : IpcService
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public IAudioOutManagerForDebugger(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}