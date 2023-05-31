namespace Ryujinx.HLE.HOS.Services.Am.Spsm
{
    [Service("spsm")]
    sealed class IPowerStateInterface : IpcService
    {
        public IPowerStateInterface(ServiceCtx context) { }
    }
}