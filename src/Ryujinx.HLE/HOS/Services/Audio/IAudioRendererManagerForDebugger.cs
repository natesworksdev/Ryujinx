namespace Ryujinx.HLE.HOS.Services.Audio
{
    [Service("audren:d")]
    class IAudioRendererManagerForDebugger : IpcService
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public IAudioRendererManagerForDebugger(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}