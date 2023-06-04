namespace Ryujinx.HLE.HOS.Services.Pm
{
    [Service("pm:bm")]
    class IBootModeInterface : IpcService
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public IBootModeInterface(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}