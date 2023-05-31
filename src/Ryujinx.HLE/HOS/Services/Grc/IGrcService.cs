namespace Ryujinx.HLE.HOS.Services.Grc
{
    [Service("grc:c")] // 4.0.0+
    sealed class IGrcService : IpcService
    {
        public IGrcService(ServiceCtx context) { }
    }
}