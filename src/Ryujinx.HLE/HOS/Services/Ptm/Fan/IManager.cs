namespace Ryujinx.HLE.HOS.Services.Ptm.Fan
{
    [Service("fan")]
    sealed class IManager : IpcService
    {
        public IManager(ServiceCtx context) { }
    }
}