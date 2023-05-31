namespace Ryujinx.HLE.HOS.Services.Audio
{
    [Service("audren:d")]
    sealed class IAudioRendererManagerForDebugger : IpcService
    {
        public IAudioRendererManagerForDebugger(ServiceCtx context) { }
    }
}