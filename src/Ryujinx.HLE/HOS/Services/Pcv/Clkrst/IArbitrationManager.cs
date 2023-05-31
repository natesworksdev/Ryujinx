namespace Ryujinx.HLE.HOS.Services.Pcv.Clkrst
{
    [Service("clkrst:a")] // 8.0.0+
    sealed class IArbitrationManager : IpcService
    {
        public IArbitrationManager(ServiceCtx context) { }
    }
}