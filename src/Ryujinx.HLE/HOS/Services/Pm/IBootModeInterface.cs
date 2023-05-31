namespace Ryujinx.HLE.HOS.Services.Pm
{
    [Service("pm:bm")]
    sealed class IBootModeInterface : IpcService
    {
        public IBootModeInterface(ServiceCtx context) { }
    }
}