namespace Ryujinx.HLE.HOS.Services.Arp
{
    [Service("arp:w")]
    sealed class IWriter : IpcService
    {
        public IWriter(ServiceCtx context) { }
    }
}