namespace Ryujinx.HLE.HOS.Services.Ectx
{
    [Service("ectx:aw")] // 11.0.0+
    class IWriterForApplication : IpcService
    {
#pragma warning disable IDE0060
        public IWriterForApplication(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}
