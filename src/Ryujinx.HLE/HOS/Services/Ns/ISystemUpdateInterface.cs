namespace Ryujinx.HLE.HOS.Services.Ns
{
    [Service("ns:su")]
    sealed class ISystemUpdateInterface : IpcService
    {
        public ISystemUpdateInterface(ServiceCtx context) { }
    }
}