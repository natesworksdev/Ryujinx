namespace Ryujinx.HLE.HOS.Services.Ns
{
    [Service("ns:dev")]
    sealed class IDevelopInterface : IpcService
    {
        public IDevelopInterface(ServiceCtx context) { }
    }
}