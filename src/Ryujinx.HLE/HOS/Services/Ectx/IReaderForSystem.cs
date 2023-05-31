namespace Ryujinx.HLE.HOS.Services.Ectx
{
    [Service("ectx:r")] // 11.0.0+
    sealed class IReaderForSystem : IpcService
    {
        public IReaderForSystem(ServiceCtx context) { }
    }
}