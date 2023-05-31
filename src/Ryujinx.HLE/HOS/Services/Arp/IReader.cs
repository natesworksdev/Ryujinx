namespace Ryujinx.HLE.HOS.Services.Arp
{
    [Service("arp:r")]
    sealed class IReader : IpcService
    {
        public IReader(ServiceCtx context) { }
    }
}