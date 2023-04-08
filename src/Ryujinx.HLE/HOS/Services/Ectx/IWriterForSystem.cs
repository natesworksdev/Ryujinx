namespace Ryujinx.HLE.HOS.Services.Ectx
{
    [Service("ectx:w")] // 11.0.0+
    class IWriterForSystem : IpcService
    {
#pragma warning disable IDE0060
        public IWriterForSystem(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}
