namespace Ryujinx.HLE.HOS.Services.Audio
{
    [Service("audrec:d")]
    class IFinalOutputRecorderManagerForDebugger : IpcService
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public IFinalOutputRecorderManagerForDebugger(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}