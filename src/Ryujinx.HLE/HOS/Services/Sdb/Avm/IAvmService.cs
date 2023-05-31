namespace Ryujinx.HLE.HOS.Services.Am.Tcap
{
    [Service("avm")] // 6.0.0+
    sealed class IAvmService : IpcService
    {
        public IAvmService(ServiceCtx context) { }
    }
}