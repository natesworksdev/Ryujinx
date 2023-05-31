namespace Ryujinx.HLE.HOS.Services.Audio
{
    [Service("audout:d")]
    sealed class IAudioOutManagerForDebugger : IpcService
    {
        public IAudioOutManagerForDebugger(ServiceCtx context) { }
    }
}