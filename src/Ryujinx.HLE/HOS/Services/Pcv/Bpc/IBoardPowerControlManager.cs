namespace Ryujinx.HLE.HOS.Services.Pcv.Bpc
{
    [Service("bpc")]
    sealed class IBoardPowerControlManager : IpcService
    {
        public IBoardPowerControlManager(ServiceCtx context) { }
    }
}