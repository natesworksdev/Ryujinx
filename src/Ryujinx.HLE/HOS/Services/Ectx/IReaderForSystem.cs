namespace Ryujinx.HLE.HOS.Services.Ectx
{
    [Service("ectx:r")] // 11.0.0+
    class IReaderForSystem : IpcService
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public IReaderForSystem(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}