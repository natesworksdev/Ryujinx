namespace Ryujinx.HLE.HOS.Services.Psc
{
    [Service("psc:l")] // 9.0.0+
    sealed class IPmUnknown : IpcService
    {
        public IPmUnknown(ServiceCtx context) { }
    }
}