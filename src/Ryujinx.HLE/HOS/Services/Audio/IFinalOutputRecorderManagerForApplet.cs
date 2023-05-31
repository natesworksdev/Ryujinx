namespace Ryujinx.HLE.HOS.Services.Audio
{
    [Service("audrec:a")]
    sealed class IFinalOutputRecorderManagerForApplet : IpcService
    {
        public IFinalOutputRecorderManagerForApplet(ServiceCtx context) { }
    }
}