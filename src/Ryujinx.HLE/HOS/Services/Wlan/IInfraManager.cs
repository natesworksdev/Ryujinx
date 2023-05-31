namespace Ryujinx.HLE.HOS.Services.Wlan
{
    [Service("wlan:inf")]
    sealed class IInfraManager : IpcService
    {
        public IInfraManager(ServiceCtx context) { }
    }
}