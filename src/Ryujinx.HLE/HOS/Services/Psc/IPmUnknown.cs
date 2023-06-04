namespace Ryujinx.HLE.HOS.Services.Psc
{
    [Service("psc:l")] // 9.0.0+
    class IPmUnknown : IpcService
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public IPmUnknown(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}