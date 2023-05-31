namespace Ryujinx.HLE.HOS.Services.Am.Idle
{
    [Service("idle:sys")]
    sealed class IPolicyManagerSystem : IpcService
    {
        public IPolicyManagerSystem(ServiceCtx context) { }
    }
}