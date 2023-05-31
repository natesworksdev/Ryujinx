namespace Ryujinx.HLE.HOS.Services.Hid.Irs
{
    [Service("irs:sys")]
    sealed class IIrSensorSystemServer : IpcService
    {
        public IIrSensorSystemServer(ServiceCtx context) { }
    }
}