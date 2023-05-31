namespace Ryujinx.HLE.HOS.Services.Sockets.Ethc
{
    [Service("ethc:c")]
    sealed class IEthInterface : IpcService
    {
        public IEthInterface(ServiceCtx context) { }
    }
}