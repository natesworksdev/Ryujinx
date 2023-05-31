namespace Ryujinx.HLE.HOS.Services.Audio
{
    [Service("audrec:u")]
    sealed class IFinalOutputRecorderManager : IpcService
    {
        public IFinalOutputRecorderManager(ServiceCtx context) { }
    }
}