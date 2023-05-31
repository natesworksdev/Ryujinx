namespace Ryujinx.HLE.HOS.Services.Ectx
{
    [Service("ectx:aw")] // 11.0.0+
    sealed class IWriterForApplication : IpcService
    {
        public IWriterForApplication(ServiceCtx context) { }
    }
}