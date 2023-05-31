namespace Ryujinx.HLE.HOS.Services.Audio
{
    [Service("audrec:d")]
    sealed class IFinalOutputRecorderManagerForDebugger : IpcService
    {
        public IFinalOutputRecorderManagerForDebugger(ServiceCtx context) { }
    }
}