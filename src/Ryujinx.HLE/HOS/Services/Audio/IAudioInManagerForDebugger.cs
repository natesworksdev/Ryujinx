namespace Ryujinx.HLE.HOS.Services.Audio
{
    [Service("audin:d")]
    sealed class IAudioInManagerForDebugger : IpcService
    {
        public IAudioInManagerForDebugger(ServiceCtx context) { }
    }
}