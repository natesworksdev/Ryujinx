namespace Ryujinx.HLE.HOS.Services.Pcv.Rgltr
{
    [Service("rgltr")] // 8.0.0+
    sealed class IRegulatorManager : IpcService
    {
        public IRegulatorManager(ServiceCtx context) { }
    }
}