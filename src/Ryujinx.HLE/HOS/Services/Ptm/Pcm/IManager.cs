namespace Ryujinx.HLE.HOS.Services.Ptm.Pcm
{
    [Service("pcm")]
    sealed class IManager : IpcService
    {
        public IManager(ServiceCtx context) { }
    }
}